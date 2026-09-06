using System.Text.Json;
using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents;
using Ecunexo.Billing.Core.Documents.Ports;
using Ecunexo.Billing.Core.Sri;
using Ecunexo.Billing.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecunexo.Billing.Infrastructure.Persistence.Repositories;

public sealed class EfInvoiceRepository(BillingDbContext db) : IInvoiceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task SaveNewAsync(
        SalesDocument document,
        Guid emitterId,
        Guid? tenantId,
        Guid? createdByUserId = null,
        CancellationToken cancellationToken = default)
    {
        db.Invoices.Add(InvoicePersistenceMapper.ToEntity(document, emitterId, tenantId, createdByUserId));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid?> GetCreatedByUserIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        await db.Invoices.AsNoTracking()
            .Where(x => x.Id == invoiceId)
            .Select(x => x.CreatedByUserId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<SalesDocument?> GetDomainAsync(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await LoadInvoiceAsync(emitterId, invoiceId, cancellationToken).ConfigureAwait(false);
        return entity is null ? null : InvoicePersistenceMapper.ToDomain(entity);
    }

    public async Task<(SalesDocument Document, Guid? TenantId)?> GetDomainWithTenantAsync(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await LoadInvoiceAsync(emitterId, invoiceId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return null;
        return (InvoicePersistenceMapper.ToDomain(entity), entity.TenantId);
    }

    public async Task<(SalesDocument Document, byte[]? UnsignedXml, byte[]? SignedXml)?> GetWithXmlAsync(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await LoadInvoiceAsync(emitterId, invoiceId, cancellationToken).ConfigureAwait(false);
        if (entity is null) return null;
        return (
            InvoicePersistenceMapper.ToDomain(entity),
            entity.XmlArtifact?.UnsignedXml,
            entity.XmlArtifact?.SignedXml);
    }

    public async Task UpdateAfterSignAsync(
        ElectronicDocument document,
        byte[] unsignedXml,
        byte[] signedXml,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.Invoices
            .Include(x => x.XmlArtifact)
            .FirstOrDefaultAsync(x => x.Id == document.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Factura no encontrada.");

        entity.State = document.State.ToString();
        entity.AccessKey = document.AccessKey?.Value;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.XmlArtifact ??= new InvoiceXmlArtifactEntity { InvoiceId = document.Id };
        entity.XmlArtifact.UnsignedXml = unsignedXml;
        entity.XmlArtifact.SignedXml = signedXml;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateStateAsync(
        Guid invoiceId,
        SriDocumentState state,
        SriTransmissionResult? transmission,
        string? authorizedXml = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.Invoices
            .Include(x => x.XmlArtifact)
            .FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Factura no encontrada.");

        entity.State = state.ToString();
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        if (transmission is not null)
        {
            entity.SriTransmissionState = transmission.State.ToString();
            entity.SriMessagesJson = JsonSerializer.Serialize(
                transmission.Messages.Select(m => new
                {
                    m.Identifier,
                    m.Text,
                    m.Detail,
                    Type = m.Type.ToString(),
                }),
                JsonOptions);
            if (!string.IsNullOrWhiteSpace(transmission.AuthorizedXml) || authorizedXml is not null)
            {
                entity.XmlArtifact ??= new InvoiceXmlArtifactEntity { InvoiceId = invoiceId };
                entity.XmlArtifact.AuthorizedXml = authorizedXml ?? transmission.AuthorizedXml;
                entity.XmlArtifact.AuthorizationDate = transmission.AuthorizationDate?.ToUniversalTime();
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<InvoiceListResult> ListAsync(
        InvoiceListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var q = db.Invoices.AsNoTracking().Where(x => x.EmitterId == query.EmitterId);
        if (query.From is not null)
            q = q.Where(x => x.IssueDate >= query.From);
        if (query.To is not null)
            q = q.Where(x => x.IssueDate <= query.To);
        if (!string.IsNullOrWhiteSpace(query.State))
            q = q.Where(x => x.State == query.State);
        if (query.CreatedByUserId is not null)
            q = q.Where(x => x.CreatedByUserId == query.CreatedByUserId);

        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await q
            .OrderByDescending(x => x.IssueDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var resendHeads = await ResolveResendHeadIdsAsync(query.EmitterId, cancellationToken)
            .ConfigureAwait(false);

        var facturaIds = rows
            .Where(r => r.DocumentType == DocumentTypeCode.Factura.Value)
            .Select(r => r.Id)
            .ToList();
        var creditNotes = await db.Invoices.AsNoTracking()
            .Where(x => x.ModifiedInvoiceId != null && facturaIds.Contains(x.ModifiedInvoiceId.Value))
            .Select(x => new { x.ModifiedInvoiceId, x.State })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var notesByInvoice = creditNotes
            .GroupBy(x => x.ModifiedInvoiceId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = rows.Select(r =>
        {
            var cp = JsonSerializer.Deserialize<CpDto>(r.CounterpartyJson, JsonOptions);
            var canResend = resendHeads.Contains(r.Id)
                && InvoiceSriResendRules.IsResendableState(r.State);
            var related = notesByInvoice.GetValueOrDefault(r.Id);
            var hasBlocking = related?.Any(n => ElectronicCreditNote.BlocksNewCreditNote(n.State)) == true;
            var isVoided = related?.Any(n => n.State == nameof(SriDocumentState.Authorized)) == true;
            var canVoid = r.DocumentType == DocumentTypeCode.Factura.Value
                && r.State == nameof(SriDocumentState.Authorized)
                && !hasBlocking;
            return new InvoiceListItem(
                r.Id,
                r.IssueDate,
                r.Establishment,
                r.EmissionPoint,
                r.Sequential,
                r.AccessKey,
                cp?.BusinessName ?? string.Empty,
                cp?.Identification ?? string.Empty,
                r.GrandTotal,
                r.State,
                r.SriTransmissionState,
                r.CreatedAt,
                canResend,
                r.DocumentType,
                canVoid,
                isVoided,
                r.ModifiedInvoiceId,
                cp?.IdentificationType ?? "04");
        }).ToList();

        return new InvoiceListResult(items, total, page, pageSize);
    }

    public async Task<Guid?> GetNextResendableInvoiceIdAsync(
        Guid emitterId,
        string establishment,
        string emissionPoint,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        var authorized = nameof(SriDocumentState.Authorized);
        var row = await db.Invoices.AsNoTracking()
            .Where(x =>
                x.EmitterId == emitterId
                && x.Establishment == establishment
                && x.EmissionPoint == emissionPoint
                && x.DocumentType == documentType
                && x.State != authorized)
            .OrderBy(x => x.Sequential)
            .Select(x => new { x.Id, x.State })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
            return null;

        return row.Id;
    }

    public async Task<Guid?> FindBlockingCreditNoteIdAsync(
        Guid modifiedInvoiceId,
        CancellationToken cancellationToken = default)
    {
        var notes = await db.Invoices.AsNoTracking()
            .Where(x =>
                x.ModifiedInvoiceId == modifiedInvoiceId
                && x.DocumentType == DocumentTypeCode.NotaCredito.Value)
            .Select(x => new { x.Id, x.State })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return notes.FirstOrDefault(n => ElectronicCreditNote.BlocksNewCreditNote(n.State))?.Id;
    }

    private async Task<HashSet<Guid>> ResolveResendHeadIdsAsync(
        Guid emitterId,
        CancellationToken cancellationToken)
    {
        var authorized = nameof(SriDocumentState.Authorized);
        var pending = await db.Invoices.AsNoTracking()
            .Where(x => x.EmitterId == emitterId && x.State != authorized)
            .Select(x => new { x.Id, x.Establishment, x.EmissionPoint, x.DocumentType, x.Sequential, x.State })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return pending
            .GroupBy(x => (x.Establishment, x.EmissionPoint, x.DocumentType))
            .Select(g => g.OrderBy(x => x.Sequential).First())
            .Where(x => InvoiceSriResendRules.IsResendableState(x.State))
            .Select(x => x.Id)
            .ToHashSet();
    }

    public async Task<byte[]?> GetSignedXmlAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var art = await db.InvoiceXmlArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId, cancellationToken)
            .ConfigureAwait(false);
        return art?.SignedXml;
    }

    public async Task<IReadOnlyList<SriMessageResponseDto>> GetMessagesAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var json = await db.Invoices.AsNoTracking()
            .Where(x => x.Id == invoiceId)
            .Select(x => x.SriMessagesJson)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        var rows = JsonSerializer.Deserialize<List<MsgDto>>(json, JsonOptions) ?? [];
        return rows
            .Select(m => new SriMessageResponseDto(m.Identifier, m.Text, m.Detail, m.Type))
            .ToList();
    }

    public async Task<string?> GetTransmissionStateAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        await db.Invoices.AsNoTracking()
            .Where(x => x.Id == invoiceId)
            .Select(x => x.SriTransmissionState)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<DateTimeOffset?> GetAuthorizationDateAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        await db.InvoiceXmlArtifacts.AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId)
            .Select(x => x.AuthorizationDate)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    private async Task<ElectronicInvoiceEntity?> LoadInvoiceAsync(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken) =>
        await db.Invoices
            .Include(x => x.Lines)
            .Include(x => x.XmlArtifact)
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.EmitterId == emitterId, cancellationToken)
            .ConfigureAwait(false);

    private sealed record CpDto(string IdentificationType, string Identification, string BusinessName, string? Address);

    private sealed record MsgDto(string Identifier, string Text, string? Detail, string Type);
}
