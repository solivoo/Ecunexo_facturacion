using Ecunexo.Billing.Api.Contracts.Emitters;
using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents;
using Ecunexo.Billing.Core.Documents.Ports;
using Ecunexo.Billing.Core.Emitter;
using Microsoft.AspNetCore.Mvc;

namespace Ecunexo.Billing.Api.Controllers;

[ApiController]
[Route("api/v1/emitters")]
public sealed class EmittersController(IEmitterRepository emitterRepository) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateEmitterResponse>> Create(
        [FromBody] CreateEmitterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ruc = Ruc.Create(request.Ruc);

            Guid? tenantId = null;
            if (Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader)
                && Guid.TryParse(tenantHeader.FirstOrDefault(), out var tid))
            {
                tenantId = tid;
            }

            var existingId = await emitterRepository
                .FindPreferredIdByRucAsync(ruc.Value, tenantId, cancellationToken)
                .ConfigureAwait(false);
            if (existingId is Guid knownId)
            {
                await emitterRepository
                    .SyncIdentityAsync(
                        knownId,
                        request.BusinessName,
                        request.MainAddress,
                        request.TradeName,
                        cancellationToken)
                    .ConfigureAwait(false);
                return Ok(new CreateEmitterResponse(knownId, ruc.Value));
            }

            var emitter = Emitter.Create(
                ruc,
                request.BusinessName,
                request.MainAddress,
                request.TradeName);

            var emitterId = await emitterRepository
                .AddAsync(emitter, tenantId, "001", "001", cancellationToken)
                .ConfigureAwait(false);

            return Created(
                $"/api/v1/emitters/{emitterId}",
                new CreateEmitterResponse(emitterId, emitter.Ruc.Value));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{emitterId:guid}")]
    public async Task<ActionResult<CreateEmitterResponse>> Get(
        Guid emitterId,
        CancellationToken cancellationToken)
    {
        var emitter = await emitterRepository.GetAsync(emitterId, cancellationToken).ConfigureAwait(false);
        if (emitter is null)
            return NotFound("Emisor no encontrado.");

        return Ok(new CreateEmitterResponse(emitterId, emitter.Ruc.Value));
    }

    [HttpGet("{emitterId:guid}/sequential-next")]
    public async Task<ActionResult<SequentialNextResponse>> PeekSequential(
        Guid emitterId,
        [FromQuery] string? establishment,
        [FromQuery] string? emissionPoint,
        CancellationToken cancellationToken)
    {
        var emitter = await emitterRepository.GetAsync(emitterId, cancellationToken).ConfigureAwait(false);
        if (emitter is null)
            return NotFound("Emisor no encontrado.");

        try
        {
            var estab = string.IsNullOrWhiteSpace(establishment) ? "001" : establishment;
            var pto = string.IsNullOrWhiteSpace(emissionPoint) ? "001" : emissionPoint;
            await emitterRepository
                .EnsureEstablishmentPointAsync(
                    emitterId,
                    estab,
                    pto,
                    DocumentTypeCode.Factura.Value,
                    emitter.MainAddress,
                    cancellationToken)
                .ConfigureAwait(false);

            var next = await emitterRepository
                .PeekNextSequentialAsync(
                    emitterId,
                    estab,
                    pto,
                    DocumentTypeCode.Factura.Value,
                    cancellationToken)
                .ConfigureAwait(false);

            return Ok(new SequentialNextResponse(next, NormalizeCode3(estab), NormalizeCode3(pto)));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{emitterId:guid}/sequential-next")]
    public async Task<ActionResult<SequentialNextResponse>> SetSequential(
        Guid emitterId,
        [FromBody] SetSequentialNextRequest request,
        CancellationToken cancellationToken)
    {
        var emitter = await emitterRepository.GetAsync(emitterId, cancellationToken).ConfigureAwait(false);
        if (emitter is null)
            return NotFound("Emisor no encontrado.");

        try
        {
            var estab = string.IsNullOrWhiteSpace(request.Establishment) ? "001" : request.Establishment;
            var pto = string.IsNullOrWhiteSpace(request.EmissionPoint) ? "001" : request.EmissionPoint;

            await emitterRepository
                .EnsureEstablishmentPointAsync(
                    emitterId,
                    estab,
                    pto,
                    DocumentTypeCode.Factura.Value,
                    string.IsNullOrWhiteSpace(request.Address) ? emitter.MainAddress : request.Address,
                    cancellationToken)
                .ConfigureAwait(false);

            var next = await emitterRepository
                .SetNextSequentialAsync(
                    emitterId,
                    estab,
                    pto,
                    DocumentTypeCode.Factura.Value,
                    request.NextSequential,
                    cancellationToken)
                .ConfigureAwait(false);

            return Ok(new SequentialNextResponse(next, NormalizeCode3(estab), NormalizeCode3(pto)));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static string NormalizeCode3(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return "001";
        return digits.Length >= 3 ? digits[^3..] : digits.PadLeft(3, '0');
    }

    [HttpGet("{emitterId:guid}/establishments")]
    public async Task<ActionResult<IReadOnlyList<EstablishmentResponse>>> ListEstablishments(
        Guid emitterId,
        CancellationToken cancellationToken)
    {
        var emitter = await emitterRepository.GetAsync(emitterId, cancellationToken).ConfigureAwait(false);
        if (emitter is null)
            return NotFound("Emisor no encontrado.");

        var rows = await emitterRepository.ListEstablishmentsAsync(emitterId, cancellationToken)
            .ConfigureAwait(false);
        return Ok(rows.Select(r => new EstablishmentResponse(r.Code, r.Address)).ToList());
    }

    [HttpPost("{emitterId:guid}/establishments")]
    public async Task<IActionResult> AddEstablishment(
        Guid emitterId,
        [FromBody] AddEstablishmentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await emitterRepository
                .AddEstablishmentAsync(
                    emitterId,
                    request.Code,
                    request.Address,
                    request.EmissionPoint ?? "001",
                    DocumentTypeCode.Factura.Value,
                    cancellationToken)
                .ConfigureAwait(false);
            return Created($"/api/v1/emitters/{emitterId}/establishments/{request.Code}", null);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{emitterId:guid}/certificates")]
    public async Task<IActionResult> AssignCertificate(
        Guid emitterId,
        [FromBody] AssignCertificateRequest request,
        CancellationToken cancellationToken)
    {
        var emitter = await emitterRepository.GetAsync(emitterId, cancellationToken).ConfigureAwait(false);
        if (emitter is null)
            return NotFound("Emisor no encontrado.");

        try
        {
            var location = Enum.TryParse<CertificateLocation>(request.Location, ignoreCase: true, out var parsed)
                ? parsed
                : throw new ArgumentException("Ubicación de certificado inválida.");

            var storage = CertificateStorageRef.Create(request.Provider, request.SecretName, location);
            var certificate = SigningCertificate.Create(
                emitter.Ruc,
                request.SerialNumber,
                request.NotBefore,
                request.NotAfter,
                storage);

            emitter.AssignCertificate(certificate);
            await emitterRepository.SaveCertificateAsync(emitter, cancellationToken).ConfigureAwait(false);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}

public sealed record EstablishmentResponse(string Code, string Address);

public sealed record SequentialNextResponse(string NextSequential, string Establishment, string EmissionPoint);

public sealed record SetSequentialNextRequest(
    string NextSequential,
    string? Establishment,
    string? EmissionPoint,
    string? Address);

public sealed record AddEstablishmentRequest(
    string Code,
    string Address,
    string? EmissionPoint);
