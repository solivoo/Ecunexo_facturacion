using EmitterAgg = Ecunexo.Billing.Core.Emitter.Emitter;

namespace Ecunexo.Billing.Core.Sri.Ports;

/// <summary>
/// En ambiente Test, si hay RUC inscrito en celcer, el XML no usa el RUC inventado del tenant.
/// </summary>
public interface ISriEmissionIdentityResolver
{
    SriEmissionIdentity Resolve(
        EmitterAgg emitter,
        string? requestedEstablishment,
        string? requestedEmissionPoint);
}
