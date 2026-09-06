using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents.Ports;
using Ecunexo.Billing.Core.Emitter;
using Ecunexo.Billing.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecunexo.Billing.Infrastructure.Persistence.Repositories;

public sealed class EfEmitterRepository(BillingDbContext db) : IEmitterRepository
{
    public async Task<Emitter?> GetAsync(Guid emitterId, CancellationToken cancellationToken = default)
    {
        var entity = await db.Emitters
            .Include(x => x.Establishments)
            .ThenInclude(x => x.Points)
            .FirstOrDefaultAsync(x => x.Id == emitterId, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<Guid?> FindPreferredIdByRucAsync(
        string ruc,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var normalizedRuc = (ruc ?? string.Empty).Trim();
        if (normalizedRuc.Length == 0)
            return null;

        // Preferir siempre el emisor con más facturas (misma empresa / mismo RUC).
        IQueryable<EmitterEntity> scope = db.Emitters.AsNoTracking()
            .Where(e => e.Ruc == normalizedRuc && e.Active);

        if (tenantId is Guid tid)
        {
            var withTenant = scope.Where(e => e.TenantId == tid);
            if (await withTenant.AnyAsync(cancellationToken).ConfigureAwait(false))
                scope = withTenant;
        }

        var preferred = await (
                from e in scope
                join i in db.Invoices.AsNoTracking() on e.Id equals i.EmitterId into inv
                orderby inv.Count() descending, e.CreatedAt
                select new { e.Id, e.TenantId })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (preferred is null)
            return null;

        if (tenantId is Guid assignTenant && preferred.TenantId is null)
        {
            var entity = await db.Emitters.FirstOrDefaultAsync(e => e.Id == preferred.Id, cancellationToken)
                .ConfigureAwait(false);
            if (entity is not null && entity.TenantId is null)
            {
                entity.TenantId = assignTenant;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return preferred.Id;
    }

    public async Task SyncIdentityAsync(
        Guid emitterId,
        string businessName,
        string mainAddress,
        string? tradeName,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.Emitters
            .FirstOrDefaultAsync(e => e.Id == emitterId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return;
        }

        var name = businessName.Trim();
        var address = mainAddress.Trim();
        var trade = string.IsNullOrWhiteSpace(tradeName) ? null : tradeName.Trim();
        if (entity.BusinessName == name
            && entity.MainAddress == address
            && entity.TradeName == trade)
        {
            return;
        }

        if (name.Length > 0)
        {
            entity.BusinessName = name;
        }

        if (address.Length > 0)
        {
            entity.MainAddress = address;
        }

        entity.TradeName = trade;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid> AddAsync(
        Emitter emitter,
        Guid? tenantId,
        string defaultEstablishmentCode,
        string defaultEmissionPoint,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var estabCode = NormalizeCode3(defaultEstablishmentCode);
        var pto = NormalizeCode3(defaultEmissionPoint);
        var estabId = Guid.CreateVersion7();
        var entity = new EmitterEntity
        {
            Id = emitter.Id,
            TenantId = tenantId,
            Ruc = emitter.Ruc.Value,
            BusinessName = emitter.BusinessName,
            TradeName = emitter.TradeName,
            MainAddress = emitter.MainAddress,
            Active = emitter.Active,
            CreatedAt = now,
            Establishments =
            [
                new EstablishmentEntity
                {
                    Id = estabId,
                    EmitterId = emitter.Id,
                    Code = estabCode,
                    Address = emitter.MainAddress,
                    Points =
                    [
                        new EmissionPointConfigEntity
                        {
                            Id = Guid.CreateVersion7(),
                            EstablishmentId = estabId,
                            EmissionPoint = pto,
                            DocumentType = DocumentTypeCode.Factura.Value,
                            LastSequential = 0,
                        },
                    ],
                },
            ],
        };

        db.Emitters.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return emitter.Id;
    }

    public async Task SaveCertificateAsync(Emitter emitter, CancellationToken cancellationToken = default)
    {
        var entity = await db.Emitters.FirstOrDefaultAsync(x => x.Id == emitter.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Emisor no encontrado.");

        var cert = emitter.GetCertificate();
        entity.CertSerialNumber = cert.SerialNumber;
        entity.CertNotBefore = cert.NotBefore;
        entity.CertNotAfter = cert.NotAfter;
        entity.CertProvider = cert.StorageRef.Provider;
        entity.CertSecretName = cert.StorageRef.KeyId;
        entity.CertLocation = cert.StorageRef.Location.ToString();
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task EnsureEstablishmentPointAsync(
        Guid emitterId,
        string establishmentCode,
        string emissionPoint,
        string documentType,
        string address,
        CancellationToken cancellationToken = default)
    {
        var estabCode = NormalizeCode3(establishmentCode);
        var pto = NormalizeCode3(emissionPoint);

        var estab = await db.Establishments
            .Include(x => x.Points)
            .FirstOrDefaultAsync(x => x.EmitterId == emitterId && x.Code == estabCode, cancellationToken)
            .ConfigureAwait(false);

        if (estab is null)
        {
            estab = new EstablishmentEntity
            {
                Id = Guid.CreateVersion7(),
                EmitterId = emitterId,
                Code = estabCode,
                Address = string.IsNullOrWhiteSpace(address) ? "Sin dirección" : address,
            };
            db.Establishments.Add(estab);
        }

        if (!estab.Points.Any(p => p.EmissionPoint == pto && p.DocumentType == documentType))
        {
            db.EmissionPointConfigs.Add(new EmissionPointConfigEntity
            {
                Id = Guid.CreateVersion7(),
                EstablishmentId = estab.Id,
                EmissionPoint = pto,
                DocumentType = documentType,
                LastSequential = 0,
            });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(string Establishment, string EmissionPoint)> ResolveRegisteredFacturaPointAsync(
        Guid emitterId,
        string? requestedEstablishment,
        string? requestedEmissionPoint,
        CancellationToken cancellationToken = default)
    {
        var documentType = DocumentTypeCode.Factura.Value;
        var hasRequest = !string.IsNullOrWhiteSpace(requestedEstablishment)
            || !string.IsNullOrWhiteSpace(requestedEmissionPoint);

        if (hasRequest)
        {
            var estabCode = NormalizeCode3(requestedEstablishment ?? "001");
            var pto = NormalizeCode3(requestedEmissionPoint ?? "001");
            var exists = await (
                    from c in db.EmissionPointConfigs.AsNoTracking()
                    join e in db.Establishments.AsNoTracking() on c.EstablishmentId equals e.Id
                    where e.EmitterId == emitterId
                          && e.Code == estabCode
                          && c.EmissionPoint == pto
                          && c.DocumentType == documentType
                    select c.Id)
                .AnyAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!exists)
            {
                throw new InvalidOperationException(
                    $"Estab {estabCode} / pto {pto} no está registrado en Billing. Configúralo en Facturación → Emisor.");
            }

            return (estabCode, pto);
        }

        var primary = await (
                from c in db.EmissionPointConfigs.AsNoTracking()
                join e in db.Establishments.AsNoTracking() on c.EstablishmentId equals e.Id
                where e.EmitterId == emitterId && c.DocumentType == documentType
                orderby e.Code, c.EmissionPoint
                select new { e.Code, c.EmissionPoint })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (primary is null)
        {
            throw new InvalidOperationException(
                "No hay punto de emisión registrado. Guarda Facturación → Emisor (estab, punto y secuencial).");
        }

        return (primary.Code, primary.EmissionPoint);
    }

    public async Task<SequentialNumber> AllocateNextSequentialAsync(
        Guid emitterId,
        string establishmentCode,
        string emissionPoint,
        string documentType,
        string? requestedSequential = null,
        CancellationToken cancellationToken = default)
    {
        var estabCode = NormalizeCode3(establishmentCode);
        var pto = NormalizeCode3(emissionPoint);
        long? requested = ParseSequentialOptional(requestedSequential);

        await using var tx = await db.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var configId = await (
                from c in db.EmissionPointConfigs.AsNoTracking()
                join e in db.Establishments.AsNoTracking() on c.EstablishmentId equals e.Id
                where e.EmitterId == emitterId
                      && e.Code == estabCode
                      && c.EmissionPoint == pto
                      && c.DocumentType == documentType
                select c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (configId == Guid.Empty)
            throw new InvalidOperationException(
                $"No hay configuración de secuencial para estab {estabCode} / pto {pto} / doc {documentType}.");

        await db.Database
            .ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM billing.emission_point_configs WHERE "Id" = {configId} FOR UPDATE""",
                cancellationToken)
            .ConfigureAwait(false);

        var config = await db.EmissionPointConfigs
            .FirstAsync(x => x.Id == configId, cancellationToken)
            .ConfigureAwait(false);

        if (config.LastSequential >= 999_999_999)
            throw new InvalidOperationException("No hay más secuenciales disponibles.");

        var nextAuto = config.LastSequential + 1;
        if (requested is not null)
        {
            if (requested.Value <= config.LastSequential)
            {
                throw new InvalidOperationException(
                    $"El secuencial {requested.Value:D9} ya fue usado o es menor/igual al último ({config.LastSequential:D9}). Próximo disponible: {nextAuto:D9}.");
            }

            // Saltar adelante hasta el solicitado (p. ej. retomar tras emisiones previas en el SRI).
            if (requested.Value > nextAuto)
                config.LastSequential = requested.Value - 1;
        }

        config.LastSequential++;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        return SequentialNumber.Create(config.LastSequential.ToString("D9"));
    }

    public async Task<string> PeekNextSequentialAsync(
        Guid emitterId,
        string establishmentCode,
        string emissionPoint,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        var estabCode = NormalizeCode3(establishmentCode);
        var pto = NormalizeCode3(emissionPoint);

        var last = await (
                from c in db.EmissionPointConfigs.AsNoTracking()
                join e in db.Establishments.AsNoTracking() on c.EstablishmentId equals e.Id
                where e.EmitterId == emitterId
                      && e.Code == estabCode
                      && c.EmissionPoint == pto
                      && c.DocumentType == documentType
                select (long?)c.LastSequential)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var next = (last ?? 0) + 1;
        if (next > 999_999_999)
            throw new InvalidOperationException("No hay más secuenciales disponibles.");

        return next.ToString("D9");
    }

    public async Task<string> SetNextSequentialAsync(
        Guid emitterId,
        string establishmentCode,
        string emissionPoint,
        string documentType,
        string nextSequential,
        CancellationToken cancellationToken = default)
    {
        var estabCode = NormalizeCode3(establishmentCode);
        var pto = NormalizeCode3(emissionPoint);
        var requested = ParseSequentialOptional(nextSequential)
            ?? throw new ArgumentException("Indique el próximo secuencial (1..999999999).");

        await using var tx = await db.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var configId = await (
                from c in db.EmissionPointConfigs.AsNoTracking()
                join e in db.Establishments.AsNoTracking() on c.EstablishmentId equals e.Id
                where e.EmitterId == emitterId
                      && e.Code == estabCode
                      && c.EmissionPoint == pto
                      && c.DocumentType == documentType
                select c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (configId == Guid.Empty)
            throw new InvalidOperationException(
                $"No hay configuración de secuencial para estab {estabCode} / pto {pto} / doc {documentType}.");

        await db.Database
            .ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM billing.emission_point_configs WHERE "Id" = {configId} FOR UPDATE""",
                cancellationToken)
            .ConfigureAwait(false);

        var config = await db.EmissionPointConfigs
            .FirstAsync(x => x.Id == configId, cancellationToken)
            .ConfigureAwait(false);

        var currentNext = config.LastSequential + 1;
        if (requested < currentNext)
        {
            throw new InvalidOperationException(
                $"No se puede rebobinar el secuencial. Próximo mínimo: {currentNext:D9} (último usado: {config.LastSequential:D9}).");
        }

        config.LastSequential = requested - 1;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        return requested.ToString("D9");
    }

    private static long? ParseSequentialOptional(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var trimmed = raw.Trim();
        if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return null;
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return null;
        if (!long.TryParse(digits, out var value) || value < 1 || value > 999_999_999)
            throw new ArgumentException("Secuencial inválido (1..999999999).");
        return value;
    }

    public async Task<IReadOnlyList<(string Code, string Address)>> ListEstablishmentsAsync(
        Guid emitterId,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.Establishments
            .AsNoTracking()
            .Where(x => x.EmitterId == emitterId)
            .OrderBy(x => x.Code)
            .Select(x => new { x.Code, x.Address })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(x => (x.Code, x.Address)).ToList();
    }

    public async Task AddEstablishmentAsync(
        Guid emitterId,
        string code,
        string address,
        string emissionPoint,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        var estabCode = NormalizeCode3(code);
        var exists = await db.Establishments
            .AnyAsync(x => x.EmitterId == emitterId && x.Code == estabCode, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
            throw new InvalidOperationException("El establecimiento ya existe en el emisor.");

        var estabId = Guid.CreateVersion7();
        db.Establishments.Add(new EstablishmentEntity
        {
            Id = estabId,
            EmitterId = emitterId,
            Code = estabCode,
            Address = address,
            Points =
            [
                new EmissionPointConfigEntity
                {
                    Id = Guid.CreateVersion7(),
                    EstablishmentId = estabId,
                    EmissionPoint = NormalizeCode3(emissionPoint),
                    DocumentType = documentType,
                    LastSequential = 0,
                },
            ],
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Emitter MapToDomain(EmitterEntity entity)
    {
        SigningCertificate? certificate = null;
        if (!string.IsNullOrWhiteSpace(entity.CertSerialNumber)
            && entity.CertNotBefore is not null
            && entity.CertNotAfter is not null
            && !string.IsNullOrWhiteSpace(entity.CertProvider)
            && !string.IsNullOrWhiteSpace(entity.CertSecretName)
            && !string.IsNullOrWhiteSpace(entity.CertLocation)
            && Enum.TryParse<CertificateLocation>(entity.CertLocation, out var loc))
        {
            var storage = CertificateStorageRef.Create(
                entity.CertProvider,
                entity.CertSecretName,
                loc);
            certificate = SigningCertificate.Create(
                Ruc.Create(entity.Ruc),
                entity.CertSerialNumber,
                entity.CertNotBefore.Value,
                entity.CertNotAfter.Value,
                storage);
        }

        var establishments = entity.Establishments
            .OrderBy(e => e.Code)
            .Select(est => Establishment.Rehydrate(
                est.Id,
                EstablishmentCode.Create(est.Code),
                est.Address,
                est.Points.Select(p => EmissionPointConfig.Rehydrate(
                    p.Id,
                    EmissionPoint.Create(p.EmissionPoint),
                    DocumentTypeCode.Create(p.DocumentType),
                    p.LastSequential))))
            .ToList();

        return Emitter.Rehydrate(
            entity.Id,
            Ruc.Create(entity.Ruc),
            entity.BusinessName,
            entity.MainAddress,
            entity.TradeName,
            entity.Active,
            establishments,
            certificate);
    }

    private static string NormalizeCode3(string raw)
    {
        var digits = new string((raw ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length >= 3) return digits[..3];
        if (digits.Length > 0) return digits.PadLeft(3, '0');
        return "001";
    }
}
