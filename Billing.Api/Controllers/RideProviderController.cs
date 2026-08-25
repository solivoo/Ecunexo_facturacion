using Ecunexo.Billing.Api.Contracts.Ride;
using Ecunexo.Billing.Api.Ride;
using Ecunexo.Billing.Infrastructure.Ride;
using Microsoft.AspNetCore.Mvc;

namespace Ecunexo.Billing.Api.Controllers;

[ApiController]
[Route("api/v1/ride-provider")]
public sealed class RideProviderController(
    RideProviderResolver resolver,
    RideProviderFileStore store) : ControllerBase
{
    [HttpGet]
    public ActionResult<RideProviderResponse> Get()
    {
        var current = resolver.Current;
        var ruc = RideProviderOptions.NormalizeRuc(current.Ruc);
        return Ok(ToResponse(ruc, current.LegalName, current.FooterLine));
    }

    [HttpPut]
    public async Task<ActionResult<RideProviderResponse>> Put(
        [FromBody] UpdateRideProviderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ruc = RideProviderOptions.NormalizeRuc(request.Ruc);
        if (!string.IsNullOrWhiteSpace(request.Ruc) && ruc is null)
            return BadRequest("El RUC del proveedor del sistema debe tener 13 dígitos.");

        var legalName = string.IsNullOrWhiteSpace(request.LegalName)
            ? "EcuNexo"
            : request.LegalName.Trim();
        var footerLine = string.IsNullOrWhiteSpace(request.FooterLine)
            ? $"Documento generado por {legalName}"
            : request.FooterLine.Trim();

        var saved = new RideProviderOptions
        {
            Ruc = ruc ?? string.Empty,
            LegalName = legalName,
            FooterLine = footerLine,
        };
        await store.SaveAsync(saved, cancellationToken).ConfigureAwait(false);
        resolver.Replace(saved);

        return Ok(ToResponse(ruc, legalName, footerLine));
    }

    private static RideProviderResponse ToResponse(string? ruc, string legalName, string footerLine)
    {
        var name = string.IsNullOrWhiteSpace(legalName) ? "EcuNexo" : legalName.Trim();
        var footer = string.IsNullOrWhiteSpace(footerLine)
            ? $"Documento generado por {name}"
            : footerLine.Trim();

        return new RideProviderResponse(ruc, name, footer, ruc is null);
    }
}
