namespace Ecunexo.Billing.Infrastructure.Persistence.Entities;

public sealed class EmissionPointConfigEntity
{
    public Guid Id { get; set; }
    public Guid EstablishmentId { get; set; }
    public string EmissionPoint { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public long LastSequential { get; set; }

    public EstablishmentEntity Establishment { get; set; } = null!;
}
