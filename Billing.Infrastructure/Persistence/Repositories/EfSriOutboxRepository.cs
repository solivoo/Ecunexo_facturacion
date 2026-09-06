using Ecunexo.Billing.Core.Documents.Ports;
using Ecunexo.Billing.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecunexo.Billing.Infrastructure.Persistence.Repositories;

public sealed class EfSriOutboxRepository(BillingDbContext db) : ISriOutboxRepository
{
    public async Task EnqueueAsync(
        Guid invoiceId,
        Guid emitterId,
        string operation,
        string environment,
        string? accessKey,
        DateTimeOffset nextAttemptAt,
        CancellationToken cancellationToken = default)
    {
        db.SriOutbox.Add(new SriOutboxEntity
        {
            Id = Guid.CreateVersion7(),
            InvoiceId = invoiceId,
            EmitterId = emitterId,
            Operation = operation,
            Status = "Pending",
            Environment = environment,
            AccessKey = accessKey,
            AttemptCount = 0,
            NextAttemptAt = nextAttemptAt.ToUniversalTime(),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SriOutboxWorkItem>> ClaimPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await db.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var take = Math.Clamp(batchSize, 1, 50);
        // Ítems InProgress abandonados (crash / claim defectuoso) vuelven a Pending.
        var staleBefore = now.AddMinutes(-5);

        await db.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE billing.sri_outbox
                SET "Status" = 'Pending',
                    "LastError" = COALESCE("LastError", 'Recuperado de InProgress abandonado')
                WHERE "Status" = 'InProgress'
                  AND "NextAttemptAt" <= {staleBefore}
                """,
                cancellationToken)
            .ConfigureAwait(false);

        // Claim atómico + RETURNING de los Ids realmente tomados (evita procesar otros InProgress).
        var claimedIds = await db.Database
            .SqlQuery<Guid>(
                $"""
                UPDATE billing.sri_outbox AS o
                SET "Status" = 'InProgress',
                    "AttemptCount" = o."AttemptCount" + 1,
                    "NextAttemptAt" = {now}
                FROM (
                    SELECT "Id"
                    FROM billing.sri_outbox
                    WHERE "Status" = 'Pending' AND "NextAttemptAt" <= {now}
                    ORDER BY "NextAttemptAt"
                    FOR UPDATE SKIP LOCKED
                    LIMIT {take}
                ) AS picked
                WHERE o."Id" = picked."Id"
                RETURNING o."Id" AS "Value"
                """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (claimedIds.Count == 0)
        {
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return [];
        }

        var rows = await db.SriOutbox.AsNoTracking()
            .Where(x => claimedIds.Contains(x.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        return rows
            .Select(r => new SriOutboxWorkItem(
                r.Id,
                r.InvoiceId,
                r.EmitterId,
                r.Operation,
                r.Environment,
                r.AccessKey,
                r.AttemptCount))
            .ToList();
    }

    public async Task MarkCompletedAsync(Guid outboxId, CancellationToken cancellationToken = default)
    {
        var row = await db.SriOutbox.FirstOrDefaultAsync(x => x.Id == outboxId, cancellationToken)
            .ConfigureAwait(false);
        if (row is null) return;
        row.Status = "Completed";
        row.CompletedAt = DateTimeOffset.UtcNow;
        row.LastError = null;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkRetryAsync(
        Guid outboxId,
        DateTimeOffset nextAttemptAt,
        string error,
        CancellationToken cancellationToken = default)
    {
        var row = await db.SriOutbox.FirstOrDefaultAsync(x => x.Id == outboxId, cancellationToken)
            .ConfigureAwait(false);
        if (row is null) return;
        row.Status = "Pending";
        row.NextAttemptAt = nextAttemptAt.ToUniversalTime();
        row.LastError = Truncate(error, 2000);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkDeadAsync(Guid outboxId, string error, CancellationToken cancellationToken = default)
    {
        var row = await db.SriOutbox.FirstOrDefaultAsync(x => x.Id == outboxId, cancellationToken)
            .ConfigureAwait(false);
        if (row is null) return;
        row.Status = "Dead";
        row.LastError = Truncate(error, 2000);
        row.CompletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RequeueAsync(
        Guid invoiceId,
        Guid emitterId,
        string operation,
        string environment,
        string? accessKey,
        CancellationToken cancellationToken = default)
    {
        var row = await db.SriOutbox
            .Where(x => x.InvoiceId == invoiceId && x.Operation == operation)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            db.SriOutbox.Add(new SriOutboxEntity
            {
                Id = Guid.CreateVersion7(),
                InvoiceId = invoiceId,
                EmitterId = emitterId,
                Operation = operation,
                Status = "Pending",
                Environment = environment,
                AccessKey = accessKey,
                AttemptCount = 0,
                NextAttemptAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            row.Status = "Pending";
            row.NextAttemptAt = DateTimeOffset.UtcNow;
            row.AttemptCount = 0;
            row.LastError = null;
            row.CompletedAt = null;
            row.AccessKey = accessKey ?? row.AccessKey;
            row.Environment = environment;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
