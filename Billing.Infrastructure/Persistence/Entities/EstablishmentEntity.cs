namespace Ecunexo.Billing.Infrastructure.Persistence.Entities;

public sealed class EstablishmentEntity
{
    public Guid Id { get; set; }
    public Guid EmitterId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public EmitterEntity Emitter { get; set; } = null!;
    public List<EmissionPointConfigEntity> Points { get; set; } = [];
}
