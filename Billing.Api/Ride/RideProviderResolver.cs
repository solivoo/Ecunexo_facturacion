using Ecunexo.Billing.Domain.Emitter;
using Ecunexo.Billing.Domain.Sri.Ports;
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

    public InvoiceXmlEmitterContext ToXmlContext(Emitter emitter)
    {
        var emission = emissionIdentity.Resolve(emitter, null, null);
        return new InvoiceXmlEmitterContext(
            emission.BusinessName,
            emission.MainAddress,
            emission.TradeName,
            SoftwareProviderRuc: ResolveRuc(emitter.Ruc.Value));
    }
}
