using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Emitter;
using Ecunexo.Billing.Core.Sri;
using Ecunexo.Billing.Core.Sri.Ports;
using Microsoft.Extensions.Options;

namespace Ecunexo.Billing.Infrastructure.Sri;

/// <summary>
/// En Test, sustituye el RUC del tenant por el inscrito en celcer (certificado + catastro SRI).
/// </summary>
public sealed class SriEmissionIdentityResolver(IOptions<SriOptions> options) : ISriEmissionIdentityResolver
{
    public SriEmissionIdentity Resolve(
        Emitter emitter,
        string? requestedEstablishment,
        string? requestedEmissionPoint)
    {
        ArgumentNullException.ThrowIfNull(emitter);

        var sri = options.Value;
        var test = sri.TestEmission;
        var environment = sri.ResolveEnvironment();

        if (environment is SriEnvironment.Test
            && test.IsConfigured()
            && !string.Equals(emitter.Ruc.Value, Digits(test.Ruc), StringComparison.Ordinal))
        {
            return new SriEmissionIdentity(
                Ruc.Create(Digits(test.Ruc)),
                test.BusinessName.Trim(),
                test.MainAddress.Trim(),
                string.IsNullOrWhiteSpace(test.TradeName) ? null : test.TradeName.Trim(),
                EstablishmentCode.Create(NormalizeCode(test.Establishment, "002")),
                EmissionPoint.Create(NormalizeCode(test.EmissionPoint, "001")),
                IsSubstituted: true);
        }

        EstablishmentCode? estab = null;
        EmissionPoint? pto = null;
        if (!string.IsNullOrWhiteSpace(requestedEstablishment))
            estab = EstablishmentCode.Create(requestedEstablishment);
        if (!string.IsNullOrWhiteSpace(requestedEmissionPoint))
            pto = EmissionPoint.Create(requestedEmissionPoint);

        return new SriEmissionIdentity(
            emitter.Ruc,
            emitter.BusinessName,
            emitter.MainAddress,
            emitter.TradeName,
            estab,
            pto,
            IsSubstituted: false);
    }

    private static string Digits(string value) => new(value.Where(char.IsDigit).ToArray());

    private static string NormalizeCode(string? value, string fallback)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return fallback;
        return digits.Length >= 3 ? digits[^3..] : digits.PadLeft(3, '0');
    }
}
