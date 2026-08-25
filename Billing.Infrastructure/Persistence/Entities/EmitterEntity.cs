namespace Ecunexo.Billing.Infrastructure.Persistence.Entities;

public sealed class EmitterEntity
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Ruc { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string MainAddress { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public string? CertSerialNumber { get; set; }
    public DateTimeOffset? CertNotBefore { get; set; }
    public DateTimeOffset? CertNotAfter { get; set; }
    public string? CertProvider { get; set; }
    public string? CertSecretName { get; set; }
    public string? CertLocation { get; set; }

    public List<EstablishmentEntity> Establishments { get; set; } = [];
}
