using Ecunexo.Billing.Core.Emitter;
using Ecunexo.Billing.Core.Sri.Ports;
using Ecunexo.Billing.Infrastructure.Ride;
using Microsoft.Extensions.Options;

namespace Ecunexo.Billing.Api.Ride;

public sealed class RideProviderResolver(
    IOptionsMonitor<RideProviderOptions> options,
    ISriEmissionIdentityResolver emissionIdentity)
{
    private RideProviderOptions? _override;

    public RideProviderOptions Current => _override ?? options.CurrentValue;

    public void Replace(RideProviderOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _override = value;
    }

    public string ResolveRuc(string emitterRuc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emitterRuc);
        return RideProviderOptions.NormalizeRuc(Current.Ruc) ?? emitterRuc.Trim();
    }

    public RideProviderInfo Snapshot(string emitterRuc)
    {
        var current = Current;
        var name = string.IsNullOrWhiteSpace(current.LegalName)
            ? "EcuNexo"
            : current.LegalName.Trim();
        var footer = string.IsNullOrWhiteSpace(current.FooterLine)
            ? $"Documento generado por {name}"
            : current.FooterLine.Trim();

        return new RideProviderInfo(ResolveRuc(emitterRuc), name, footer);
    }

    public InvoiceXmlEmitterContext ToXmlContext(
        Emitter emitter,
        string environmentCode = "1",
        string? establishmentAddress = null,
        string? obligadoContabilidad = null,
        string? contribuyenteEspecial = null)
    {
        var targetEnv = environmentCode == "2" ? Ecunexo.Billing.Core.Sri.SriEnvironment.Production : Ecunexo.Billing.Core.Sri.SriEnvironment.Test;
        var emission = emissionIdentity.Resolve(emitter, null, null, targetEnv);
        var estabAddress = !string.IsNullOrWhiteSpace(establishmentAddress)
            ? establishmentAddress.Trim()
            : emission.MainAddress;
        var obligado = !string.IsNullOrWhiteSpace(obligadoContabilidad)
            ? (obligadoContabilidad.Trim().Equals("SI", StringComparison.OrdinalIgnoreCase) ? "SI" : "NO")
            : "NO";

        return new InvoiceXmlEmitterContext(
            emission.BusinessName,
            emission.MainAddress,
            emission.TradeName,
            EnvironmentCode: environmentCode,
            SoftwareProviderRuc: ResolveRuc(emitter.Ruc.Value),
            EstablishmentAddress: estabAddress,
            ObligadoContabilidad: obligado,
            ContribuyenteEspecial: string.IsNullOrWhiteSpace(contribuyenteEspecial) ? null : contribuyenteEspecial.Trim());
    }
}
