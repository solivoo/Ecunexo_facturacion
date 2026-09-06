using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Emitter;
using Ecunexo.Billing.Core.Sri;
using Ecunexo.Billing.Core.Sri.Ports;

namespace Ecunexo.Billing.Business.Tests.Support;

internal sealed class PassthroughEmissionIdentityResolver : ISriEmissionIdentityResolver
{
    public Func<Emitter, string?, string?, SriEmissionIdentity>? ResolveOverride { get; set; }

    public SriEmissionIdentity Resolve(
        Emitter emitter,
        string? requestedEstablishment,
        string? requestedEmissionPoint)
    {
        if (ResolveOverride is not null)
            return ResolveOverride(emitter, requestedEstablishment, requestedEmissionPoint);

        EstablishmentCode? estab = string.IsNullOrWhiteSpace(requestedEstablishment)
            ? null
            : EstablishmentCode.Create(NormalizeCode3(requestedEstablishment));
        EmissionPoint? pto = string.IsNullOrWhiteSpace(requestedEmissionPoint)
            ? null
            : EmissionPoint.Create(NormalizeCode3(requestedEmissionPoint));

        return new SriEmissionIdentity(
            emitter.Ruc,
            emitter.BusinessName,
            emitter.MainAddress,
            emitter.TradeName,
            estab,
            pto,
            IsSubstituted: false);
    }

    private static string NormalizeCode3(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return "001";
        return digits.Length >= 3 ? digits[^3..] : digits.PadLeft(3, '0');
    }
}
