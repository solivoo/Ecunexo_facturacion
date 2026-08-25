using System.Text.Json;
using Ecunexo.Billing.Api.Contracts.Invoices;
using Ecunexo.Billing.Api.Http;
using Ecunexo.Billing.Api.Ride;
using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents;
using Ecunexo.Billing.Domain.Documents.Ports;
using Ecunexo.Billing.Domain.Emitter;
using Ecunexo.Billing.Domain.Emitter.Ports;
using Ecunexo.Billing.Domain.Emitter.Services;
using Ecunexo.Billing.Domain.Sri;
using Ecunexo.Billing.Domain.Sri.Ports;
using Ecunexo.Billing.Domain.TaxCatalog.Ports;
using Ecunexo.Billing.Domain.TaxRules;
using Ecunexo.Billing.Infrastructure.Sri;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Ecunexo.Billing.Api.Controllers;

[ApiController]
[Route("api/v1/emitters/{emitterId:guid}/invoices")]
public sealed class InvoicesController(
    ITaxRateRepository taxRateRepository,
    IEmitterRepository emitterRepository,
    IInvoiceRepository invoiceRepository,
    ISriOutboxRepository outboxRepository,
    IElectronicInvoiceXmlGenerator xmlGenerator,
    IElectronicCreditNoteXmlGenerator creditNoteXmlGenerator,
    IElectronicDocumentXmlValidator xmlValidator,
    IElectronicSignatureService signatureService,
    ISigningCertificateProvider signingCertificateProvider,
    ISriGateway sriGateway,
    IOptions<SriOptions> sriOptions,
    ISriEmissionIdentityResolver emissionIdentity,
    RideProviderResolver rideProviderResolver,
    SriVoidPolicy sriVoidPolicy) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private SriEnvironment SriEnv => sriOptions.Value.ResolveEnvironment();

    private SriEnvironment ResolveSriEnvironment(string? environment) =>
        Enum.TryParse<SriEnvironment>(environment, ignoreCase: true, out var parsed)
            ? parsed
            : SriEnv;

    [HttpGet]
    public async Task<ActionResult<InvoiceListResponse>> List(
        Guid emitterId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? state,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var emitter = await emitterRepository.GetAsync(emitterId, cancellationToken).ConfigureAwait(false);
        if (emitter is null)
            return NotFound("Emisor no encontrado.");

        var result = await invoiceRepository
            .ListAsync(
                new InvoiceListQuery(
                    emitterId,
                    from,
                    to,
                    state,
                    page,
                    pageSize,
                    InvoiceRequestScope.From(Request).ListCreatedByFilter),
                cancellationToken)
            .ConfigureAwait(false);

        var today = EcuadorToday();
        return Ok(new InvoiceListResponse(
            result.Items.Select(i =>
            {
                var advice = sriVoidPolicy.Advise(
                    i.IssueDate,
                    today,
                    IsConsumerFinal(i.CounterpartyIdentificationType));
                var canVoid = i.CanVoid && SriOnlineVoidEvaluator.AllowsCreditNote(advice);
                return new InvoiceListItemResponse(
                    i.InvoiceId,
                    i.IssueDate,
                    i.Establishment,
                    i.EmissionPoint,
                    i.Sequential,
                    i.AccessKey,
                    i.CounterpartyName,
                    i.CounterpartyIdentification,
                    i.GrandTotal,
                    i.State,
                    i.SriTransmissionState,
                    i.CreatedAt,
                    i.CanResend,
                    i.DocumentType,
                    canVoid,
                    i.IsVoided,
                    i.ModifiedInvoiceId,
                    i.CounterpartyIdentificationType,
                    advice.Path.ToString(),
                    advice.OnlineVoidDeadline,
                    advice.Message);
            }).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }

    [HttpGet("{invoiceId:guid}")]
    public async Task<ActionResult<InvoiceDetailResponse>> GetInvoice(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var denied = await RejectIfCannotViewAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        if (denied is not null)
            return denied;

        var emitter = await emitterRepository.GetAsync(emitterId, cancellationToken).ConfigureAwait(false);
        var loaded = await invoiceRepository.GetWithXmlAsync(emitterId, invoiceId, cancellationToken)
            .ConfigureAwait(false);
        if (emitter is null || loaded is null)
            return NotFound("Emisor o factura no encontrados.");

        var invoice = loaded.Value.Document;
        var hasSignedXml = loaded.Value.SignedXml is { Length: > 0 };
        var hasUnsignedXml = loaded.Value.UnsignedXml is { Length: > 0 };
        var provider = rideProviderResolver.Snapshot(emitter.Ruc.Value);
        var paymentTermDays = invoice is ElectronicInvoice inv ? inv.PaymentTermDays : 0;
        var creditNote = invoice as ElectronicCreditNote;
        Guid? blockingNoteId = null;
        if (invoice.DocumentType.Value == DocumentTypeCode.Factura.Value)
        {
            blockingNoteId = await invoiceRepository
                .FindBlockingCreditNoteIdAsync(invoice.Id, cancellationToken)
                .ConfigureAwait(false);
        }

        var voidAdvice = sriVoidPolicy.Advise(
            invoice.IssueDate,
            EcuadorToday(),
            IsConsumerFinal(invoice.Counterparty.IdentificationType));
        var canVoid = invoice.DocumentType.Value == DocumentTypeCode.Factura.Value
            && invoice.State == SriDocumentState.Authorized
            && blockingNoteId is null
            && SriOnlineVoidEvaluator.AllowsCreditNote(voidAdvice);
        var isVoided = false;
        if (blockingNoteId is not null)
        {
            var blocking = await invoiceRepository
                .GetDomainAsync(emitterId, blockingNoteId.Value, cancellationToken)
                .ConfigureAwait(false);
            isVoided = blocking?.State == SriDocumentState.Authorized;
        }

        return Ok(new InvoiceDetailResponse(
            invoice.Id,
            invoice.IssueDate,
            invoice.Establishment.Value,
            invoice.EmissionPoint.Value,
            invoice.Sequential.Value,
            invoice.AccessKey?.Value,
            invoice.State.ToString(),
            await invoiceRepository.GetTransmissionStateAsync(invoice.Id, cancellationToken).ConfigureAwait(false),
            invoice.SubtotalWithoutTax.Amount,
            invoice.GrandTotal.Amount,
            new InvoiceEmitterInfoResponse(
                emitter.Ruc.Value,
                emitter.BusinessName,
                emitter.TradeName,
                emitter.MainAddress),
            new InvoiceCounterpartyResponse(
                invoice.Counterparty.IdentificationType,
                invoice.Counterparty.Identification,
                invoice.Counterparty.BusinessName,
                invoice.Counterparty.Address,
                invoice.Counterparty.Email,
                invoice.Counterparty.Phone),
            invoice.Lines.Select(l => new InvoiceDetailLineResponse(
                l.LineNumber,
                l.Description,
                l.Quantity,
                l.UnitPrice.Amount,
                l.Discount.Amount,
                l.LineTotalWithoutTax.Amount,
                l.Taxes.Select(t => new InvoiceDetailLineTaxResponse(
                    t.TaxCode,
                    t.RateCode,
                    t.Rate,
                    t.TaxableBase.Amount,
                    t.Value.Amount)).ToList(),
                l.MainCode,
                l.CatalogItemId,
                l.ItemKind)).ToList(),
            invoice.TaxTotals.Select(t => new InvoiceTaxTotalResponse(
                t.TaxCode,
                t.RateCode,
                t.TaxableBase.Amount,
                t.Value.Amount)).ToList(),
            hasSignedXml,
            hasUnsignedXml,
            invoice.PaymentFormCode,
            invoice.AdditionalNote,
            paymentTermDays,
            await invoiceRepository.GetAuthorizationDateAsync(invoice.Id, cancellationToken)
                .ConfigureAwait(false),
            provider.Ruc,
            provider.LegalName,
            provider.FooterLine,
            invoice.DocumentType.Value,
            creditNote?.Motivo,
            creditNote?.ModifiedInvoiceId,
            creditNote?.ModifiedDocumentNumber,
            creditNote?.ModifiedIssueDate,
            canVoid,
            isVoided,
            voidAdvice.Path.ToString(),
            voidAdvice.OnlineVoidDeadline,
            voidAdvice.Message));
    }

    [HttpGet("{invoiceId:guid}/xml")]
    public async Task<ActionResult<InvoiceXmlDownloadResponse>> GetInvoiceXml(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var denied = await RejectIfCannotViewAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        if (denied is not null)
            return denied;

        var emitter = await emitterRepository.GetAsync(emitterId, cancellationToken).ConfigureAwait(false);
        var loaded = await invoiceRepository.GetWithXmlAsync(emitterId, invoiceId, cancellationToken)
            .ConfigureAwait(false);
        if (emitter is null || loaded is null)
            return NotFound("Emisor o factura no encontrados.");

        var invoice = loaded.Value.Document;
        string xmlText;
        var source = "preview";

        if (loaded.Value.SignedXml is { Length: > 0 })
        {
            xmlText = System.Text.Encoding.UTF8.GetString(loaded.Value.SignedXml);
            source = "signed";
        }
        else if (loaded.Value.UnsignedXml is { Length: > 0 })
        {
            xmlText = System.Text.Encoding.UTF8.GetString(loaded.Value.UnsignedXml);
            source = "unsigned";
        }
        else
        {
            var accessKey = invoice.AccessKey ?? BuildAccessKey(invoice);
            var emitterCtx = rideProviderResolver.ToXmlContext(emitter);
            var xml = BuildDocumentXml(invoice, emitterCtx, accessKey);
            xmlText = System.Text.Encoding.UTF8.GetString(xml);
        }

        return Ok(new InvoiceXmlDownloadResponse(
            invoice.Id,
            invoice.AccessKey?.Value,
            source,
            xmlText));
    }

    [HttpPost]
    public async Task<ActionResult<CreateInvoiceResponse>> CreateInvoice(
        Guid emitterId,
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var emitter = await emitterRepository.GetAsync(emitterId, cancellationToken).ConfigureAwait(false);
        if (emitter is null)
            return NotFound("Emisor no encontrado. Cree el emisor primero.");

        try
        {
            // En Test, el XML usa el RUC inscrito en celcer (no el inventado del tenant).
            var identity = emissionIdentity.Resolve(
                emitter,
                request.Establishment,
                request.EmissionPoint);

            string estabCode;
            string ptoCode;
            var sequentialOwnerId = emitterId;
            if (identity.IsSubstituted
                && identity.Establishment is not null
                && identity.EmissionPoint is not null)
            {
                estabCode = identity.Establishment.Value;
                ptoCode = identity.EmissionPoint.Value;
                sequentialOwnerId = await emitterRepository
                    .FindPreferredIdByRucAsync(identity.Ruc.Value, tenantId: null, cancellationToken)
                    .ConfigureAwait(false)
                    ?? emitterId;
                await emitterRepository
                    .EnsureEstablishmentPointAsync(
                        sequentialOwnerId,
                        estabCode,
                        ptoCode,
                        DocumentTypeCode.Factura.Value,
                        identity.MainAddress,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                (estabCode, ptoCode) = await emitterRepository
                    .ResolveRegisteredFacturaPointAsync(
                        emitterId,
                        request.Establishment,
                        request.EmissionPoint,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            var estab = EstablishmentCode.Create(estabCode);
            var pto = EmissionPoint.Create(ptoCode);

            var sequential = await emitterRepository
                .AllocateNextSequentialAsync(
                    sequentialOwnerId,
                    estab.Value,
                    pto.Value,
                    DocumentTypeCode.Factura.Value,
                    requestedSequential: null,
                    cancellationToken)
                .ConfigureAwait(false);

            var lines = request.Lines.Select(x =>
                new InvoiceLine(
                    x.LineNumber,
                    x.Description,
                    x.Quantity,
                    new Money(x.UnitPrice),
                    new Money(x.Discount),
                    new Money(x.LineTotalWithoutTax),
                    x.Taxes.Select(t =>
                        new LineTax(t.TaxCode, t.RateCode, t.Rate, new Money(t.TaxableBase), new Money(t.Value)))
                    .ToList(),
                    x.MainCode,
                    x.CatalogItemId,
                    x.ItemKind))
                .ToList();

            var invoice = ElectronicInvoice.Create(
                identity.Ruc,
                estab,
                pto,
                sequential,
                request.IssueDate,
                Counterparty.Create(
                    request.Counterparty.IdentificationType,
                    request.Counterparty.Identification,
                    request.Counterparty.BusinessName,
                    request.Counterparty.Address,
                    request.Counterparty.Email,
                    request.Counterparty.Phone),
                lines,
                taxRateRepository,
                request.PaymentFormCode,
                request.AdditionalNote,
                request.PaymentTermDays);

            await invoiceRepository
                .SaveNewAsync(
                    invoice,
                    emitterId,
                    InvoiceRequestScope.ReadTenantId(Request),
                    InvoiceRequestScope.ReadUserId(Request),
                    cancellationToken)
                .ConfigureAwait(false);

            return Created(
                $"/api/v1/emitters/{emitterId}/invoices/{invoice.Id}",
                new CreateInvoiceResponse(
                    invoice.Id,
                    invoice.State.ToString(),
                    invoice.SubtotalWithoutTax.Amount,
                    invoice.GrandTotal.Amount,
                    invoice.TaxTotals.Select(t =>
                        new InvoiceTaxTotalResponse(t.TaxCode, t.RateCode, t.TaxableBase.Amount, t.Value.Amount))
                    .ToList()));
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

    [HttpPost("{invoiceId:guid}/credit-notes")]
    public async Task<ActionResult<CreateCreditNoteResponse>> CreateCreditNote(
        Guid emitterId,
        Guid invoiceId,
        [FromBody] CreateCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        var denied = await RejectIfCannotViewAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        if (denied is not null)
            return denied;

        var emitter = await emitterRepository.GetAsync(emitterId, cancellationToken).ConfigureAwait(false);
        var document = await invoiceRepository.GetDomainAsync(emitterId, invoiceId, cancellationToken)
            .ConfigureAwait(false);
        if (emitter is null || document is null)
            return NotFound("Emisor o factura no encontrados.");

        if (document is not ElectronicInvoice invoice)
            return Conflict("Solo se puede anular una factura con nota de crédito.");

        var voidAdvice = sriVoidPolicy.Advise(
            invoice.IssueDate,
            EcuadorToday(),
            IsConsumerFinal(invoice.Counterparty.IdentificationType));
        if (!SriOnlineVoidEvaluator.AllowsCreditNote(voidAdvice))
            return Conflict(voidAdvice.Message);

        try
        {
            var blockingId = await invoiceRepository
                .FindBlockingCreditNoteIdAsync(invoice.Id, cancellationToken)
                .ConfigureAwait(false);
            if (blockingId is not null)
            {
                var existing = await invoiceRepository
                    .GetDomainAsync(emitterId, blockingId.Value, cancellationToken)
                    .ConfigureAwait(false);
                if (existing is ElectronicCreditNote existingNote
                    && existingNote.State == SriDocumentState.Draft)
                {
                    return Ok(new CreateCreditNoteResponse(
                        existingNote.Id,
                        invoice.Id,
                        existingNote.State.ToString(),
                        existingNote.Sequential.Value,
                        existingNote.GrandTotal.Amount,
                        existingNote.AccessKey?.Value,
                        null,
                        "Ya existía un borrador de nota de crédito; se reutiliza."));
                }

                return Conflict(
                    "Ya existe una nota de crédito vigente para esta factura. Si está pendiente, reenvíela al SRI.");
            }

            await emitterRepository
                .EnsureEstablishmentPointAsync(
                    emitterId,
                    invoice.Establishment.Value,
                    invoice.EmissionPoint.Value,
                    DocumentTypeCode.NotaCredito.Value,
                    emitter.MainAddress,
                    cancellationToken)
                .ConfigureAwait(false);

            var sequential = await emitterRepository
                .AllocateNextSequentialAsync(
                    emitterId,
                    invoice.Establishment.Value,
                    invoice.EmissionPoint.Value,
                    DocumentTypeCode.NotaCredito.Value,
                    requestedSequential: null,
                    cancellationToken)
                .ConfigureAwait(false);

            var issueDate = request.IssueDate
                ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));

            var note = ElectronicCreditNote.CreateFromAuthorizedInvoice(
                invoice,
                sequential,
                issueDate,
                request.Motivo);

            await invoiceRepository
                .SaveNewAsync(
                    note,
                    emitterId,
                    InvoiceRequestScope.ReadTenantId(Request),
                    InvoiceRequestScope.ReadUserId(Request),
                    cancellationToken)
                .ConfigureAwait(false);

            return Created(
                $"/api/v1/emitters/{emitterId}/invoices/{note.Id}",
                new CreateCreditNoteResponse(
                    note.Id,
                    invoice.Id,
                    note.State.ToString(),
                    note.Sequential.Value,
                    note.GrandTotal.Amount,
                    null,
                    null,
                    "Nota de crédito creada. Firme y envíe al SRI."));
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

    [HttpPost("{invoiceId:guid}/sign")]
    public async Task<ActionResult<InvoiceActionResponse>> Sign(
        Guid emitterId,
        Guid invoiceId,
        [FromQuery] string? environment,
        CancellationToken cancellationToken)
    {
        var denied = await RejectIfCannotViewAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        if (denied is not null)
            return denied;

        var emitter = await emitterRepository.GetAsync(emitterId, cancellationToken).ConfigureAwait(false);
        var loaded = await invoiceRepository.GetWithXmlAsync(emitterId, invoiceId, cancellationToken)
            .ConfigureAwait(false);

        if (emitter is null || loaded is null)
            return NotFound("Emisor o factura no encontrados.");

        var invoice = loaded.Value.Document;

        try
        {
            var accessKey = BuildAccessKey(invoice);
            var emitterCtx = rideProviderResolver.ToXmlContext(emitter);

            var xml = BuildDocumentXml(invoice, emitterCtx, accessKey);
            var validation = xmlValidator.Validate(xml, SchemaOf(invoice));
            if (!validation.IsValid)
            {
                return BadRequest(new
                {
                    message = $"El XML no cumple el XSD {SchemaName(invoice)}.",
                    errors = validation.Errors,
                });
            }

            await EnsureInfisicalCertificateBoundAsync(emitter, cancellationToken).ConfigureAwait(false);
            EmitterSigningValidator.EnsureReadyToSign(emitter);
            invoice.MarkSigned(accessKey);

            var signedXml = await signatureService.SignXmlAsync(emitterId, xml, cancellationToken)
                .ConfigureAwait(false);
            await invoiceRepository.UpdateAfterSignAsync(invoice, xml, signedXml, cancellationToken)
                .ConfigureAwait(false);

            var env = ResolveSriEnvironment(environment);
            await outboxRepository
                .EnqueueAsync(
                    invoice.Id,
                    emitterId,
                    "Reception",
                    env.ToString(),
                    accessKey.Value,
                    DateTimeOffset.UtcNow,
                    cancellationToken)
                .ConfigureAwait(false);

            return Ok(new InvoiceActionResponse(
                invoice.Id,
                invoice.State.ToString(),
                invoice.AccessKey?.Value,
                null,
                invoice.DocumentType.Value == DocumentTypeCode.NotaCredito.Value
                    ? "Nota de crédito firmada. Envío al SRI encolado."
                    : "Factura firmada. Envío al SRI encolado.",
                []));
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

    [HttpPost("{invoiceId:guid}/preview-xml")]
    public async Task<ActionResult> PreviewXml(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var denied = await RejectIfCannotViewAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        if (denied is not null)
            return denied;

        var emitter = await emitterRepository.GetAsync(emitterId, cancellationToken).ConfigureAwait(false);
        var invoice = await invoiceRepository.GetDomainAsync(emitterId, invoiceId, cancellationToken)
            .ConfigureAwait(false);

        if (emitter is null || invoice is null)
            return NotFound("Emisor o factura no encontrados.");

        try
        {
            var accessKey = invoice.AccessKey ?? BuildAccessKey(invoice);
            var emitterCtx = rideProviderResolver.ToXmlContext(emitter);

            var xml = BuildDocumentXml(invoice, emitterCtx, accessKey);
            var validation = xmlValidator.Validate(xml, SchemaOf(invoice));
            var xmlText = System.Text.Encoding.UTF8.GetString(xml);

            return Ok(new
            {
                invoiceId = invoice.Id,
                accessKey = accessKey.Value,
                isValid = validation.IsValid,
                errors = validation.Errors,
                xml = xmlText,
            });
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

    [HttpPost("{invoiceId:guid}/submit-reception")]
    public async Task<ActionResult<InvoiceActionResponse>> SubmitReception(
        Guid emitterId,
        Guid invoiceId,
        [FromQuery] string? environment,
        CancellationToken cancellationToken)
    {
        var denied = await RejectIfCannotViewAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        if (denied is not null)
            return denied;

        var signed = await invoiceRepository.GetSignedXmlAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        if (signed is null)
            return Conflict("La factura debe estar firmada antes de enviarse a recepción.");

        var env = ResolveSriEnvironment(environment);
        var result = await sriGateway.SendReceptionAsync(env, signed, cancellationToken).ConfigureAwait(false);

        var invoice = await invoiceRepository.GetDomainAsync(emitterId, invoiceId, cancellationToken)
            .ConfigureAwait(false);
        if (invoice is null)
            return NotFound("Factura no encontrada.");

        if (result.State is SriTransmissionState.Received)
        {
            invoice.MarkReceived();
            await invoiceRepository.UpdateStateAsync(invoiceId, invoice.State, result, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else if (result.State is SriTransmissionState.Returned)
        {
            await invoiceRepository.UpdateStateAsync(
                    invoiceId,
                    SriDocumentState.Returned,
                    result,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await invoiceRepository.UpdateStateAsync(invoiceId, invoice.State, result, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        return Ok(ToActionResponse(invoiceId, invoice, result, ReceptionUserMessage(result)));
    }

    [HttpPost("{invoiceId:guid}/authorize-poll")]
    public async Task<ActionResult<InvoiceActionResponse>> AuthorizePoll(
        Guid emitterId,
        Guid invoiceId,
        [FromQuery] string? environment,
        CancellationToken cancellationToken)
    {
        var denied = await RejectIfCannotViewAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        if (denied is not null)
            return denied;

        var invoice = await invoiceRepository.GetDomainAsync(emitterId, invoiceId, cancellationToken)
            .ConfigureAwait(false);
        if (invoice is null)
            return NotFound("Factura no encontrada.");

        if (invoice.AccessKey is null)
            return Conflict("La factura no tiene clave de acceso. Debe firmarse primero.");

        if (invoice.State is SriDocumentState.Received)
        {
            invoice.MarkProcessing();
            await invoiceRepository.UpdateStateAsync(invoiceId, invoice.State, null, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        var delaySeconds = Math.Clamp(sriOptions.Value.AuthorizationPollDelaySeconds, 0, 30);
        if (delaySeconds > 0)
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);

        var env = ResolveSriEnvironment(environment);
        var result = await sriGateway
            .QueryAuthorizationAsync(env, invoice.AccessKey, cancellationToken)
            .ConfigureAwait(false);

        if (result.State is SriTransmissionState.Authorized)
        {
            await invoiceRepository.UpdateStateAsync(
                    invoiceId,
                    SriDocumentState.Authorized,
                    result,
                    result.AuthorizedXml,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else if (result.State is SriTransmissionState.NotAuthorized)
        {
            await invoiceRepository.UpdateStateAsync(
                    invoiceId,
                    SriDocumentState.NotAuthorized,
                    result,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await invoiceRepository.UpdateStateAsync(
                    invoiceId,
                    invoice.State is SriDocumentState.Processing ? SriDocumentState.Processing : invoice.State,
                    result,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        invoice = await invoiceRepository.GetDomainAsync(emitterId, invoiceId, cancellationToken).ConfigureAwait(false)
            ?? invoice;

        return Ok(ToActionResponse(invoiceId, invoice, result, AuthorizationUserMessage(result)));
    }

    [HttpPost("{invoiceId:guid}/sri/retry")]
    public async Task<ActionResult<InvoiceActionResponse>> RetrySri(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var denied = await RejectIfCannotViewAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        if (denied is not null)
            return denied;

        var invoice = await invoiceRepository.GetDomainAsync(emitterId, invoiceId, cancellationToken)
            .ConfigureAwait(false);
        if (invoice is null)
            return NotFound("Factura no encontrada.");

        if (invoice.State is SriDocumentState.Authorized)
            return Conflict("La factura ya está autorizada; no requiere reenvío.");

        if (!InvoiceSriResendRules.IsResendableState(invoice.State))
            return Conflict($"El estado {invoice.State} no admite reenvío al SRI.");

        var nextId = await invoiceRepository
            .GetNextResendableInvoiceIdAsync(
                emitterId,
                invoice.Establishment.Value,
                invoice.EmissionPoint.Value,
                invoice.DocumentType.Value,
                cancellationToken)
            .ConfigureAwait(false);

        if (nextId is null || nextId != invoiceId)
            return Conflict(
                "Solo se puede reenviar la factura pendiente con el secuencial más bajo de este establecimiento y punto de emisión.");

        var messages = await invoiceRepository.GetMessagesAsync(invoiceId, cancellationToken)
            .ConfigureAwait(false);
        var operation = InvoiceSriResendRules.ResolveOutboxOperation(
            invoice.State,
            messages.Select(m => m.Identifier));
        var env = SriEnv;
        try
        {
            await outboxRepository
                .RequeueAsync(
                    invoiceId,
                    emitterId,
                    operation,
                    env.ToString(),
                    invoice.AccessKey?.Value,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }

        var hint = operation == "Authorization" && messages.Any(m => m.Identifier is "43" or "70")
            ? "Clave ya registrada en el SRI; se consulta autorización."
            : $"Reenvío {operation} encolado.";

        return Accepted(new InvoiceActionResponse(
            invoice.Id,
            invoice.State.ToString(),
            invoice.AccessKey?.Value,
            null,
            $"{hint} Secuencial {invoice.Sequential.Value}.",
            []));
    }

    [HttpGet("{invoiceId:guid}/sri-status")]
    public async Task<ActionResult<InvoiceStateResponse>> GetSriStatus(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var denied = await RejectIfCannotViewAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        if (denied is not null)
            return denied;

        var invoice = await invoiceRepository.GetDomainAsync(emitterId, invoiceId, cancellationToken)
            .ConfigureAwait(false);
        if (invoice is null)
            return NotFound("Factura no encontrada.");

        var messages = await invoiceRepository.GetMessagesAsync(invoiceId, cancellationToken)
            .ConfigureAwait(false);
        var txState = await invoiceRepository.GetTransmissionStateAsync(invoiceId, cancellationToken)
            .ConfigureAwait(false);

        return Ok(new InvoiceStateResponse(
            invoice.Id,
            invoice.State.ToString(),
            invoice.AccessKey?.Value,
            txState,
            messages.Select(m => new SriMessageResponse(m.Identifier, m.Text, m.Detail, m.Type)).ToList()));
    }

    private async Task<ActionResult?> RejectIfCannotViewAsync(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var owner = await invoiceRepository
            .GetCreatedByUserIdAsync(invoiceId, cancellationToken)
            .ConfigureAwait(false);
        return InvoiceRequestScope.From(Request).CanAccess(owner)
            ? null
            : NotFound("Emisor o factura no encontrados.");
    }

    private async Task EnsureInfisicalCertificateBoundAsync(
        Emitter emitter,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = emitter.GetCertificate();
            return;
        }
        catch (InvalidOperationException)
        {
        }

        using var cert = await signingCertificateProvider.GetAsync(cancellationToken).ConfigureAwait(false);
        var storage = CertificateStorageRef.Create(
            "Infisical",
            "keyfacturacion",
            CertificateLocation.Infisical);

        var serial = string.IsNullOrWhiteSpace(cert.SerialNumber)
            ? "INFISICAL"
            : cert.SerialNumber;

        var signingCert = SigningCertificate.Create(
            emitter.Ruc,
            serial,
            new DateTimeOffset(DateTime.SpecifyKind(cert.NotBefore, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(cert.NotAfter, DateTimeKind.Utc)),
            storage);

        emitter.AssignCertificate(signingCert);
        await emitterRepository.SaveCertificateAsync(emitter, cancellationToken).ConfigureAwait(false);
    }

    private static InvoiceActionResponse ToActionResponse(
        Guid invoiceId,
        ElectronicDocument invoice,
        SriTransmissionResult result,
        string message) =>
        new(
            invoiceId,
            invoice.State.ToString(),
            invoice.AccessKey?.Value,
            result.State.ToString(),
            message,
            result.Messages
                .Select(x => new SriMessageResponse(x.Identifier, x.Text, x.Detail, x.Type.ToString()))
                .ToList());

    private static string ReceptionUserMessage(SriTransmissionResult result) =>
        result.State switch
        {
            SriTransmissionState.Received => "Recepción aceptada por el SRI (RECIBIDA).",
            SriTransmissionState.Returned => FormatWithMessages(
                "El SRI devolvió el comprobante (DEVUELTA).",
                result),
            SriTransmissionState.TransportError => FormatWithMessages(
                "Error de transporte o respuesta no reconocida en recepción SRI.",
                result),
            _ => FormatWithMessages($"Recepción SRI: {result.State}.", result),
        };

    private static string AuthorizationUserMessage(SriTransmissionResult result) =>
        result.State switch
        {
            SriTransmissionState.Authorized => "Comprobante autorizado por el SRI.",
            SriTransmissionState.NotAuthorized => FormatWithMessages(
                "Comprobante no autorizado por el SRI.",
                result),
            SriTransmissionState.Processing => FormatWithMessages(
                "Autorización en procesamiento (aún sin resultado definitivo).",
                result),
            SriTransmissionState.TransportError => FormatWithMessages(
                "Error de transporte o respuesta no reconocida en autorización SRI.",
                result),
            _ => FormatWithMessages($"Autorización SRI: {result.State}.", result),
        };

    private static string FormatWithMessages(string prefix, SriTransmissionResult result)
    {
        if (result.Messages.Count == 0)
            return prefix;

        var detail = string.Join(
            " | ",
            result.Messages.Select(m =>
                string.IsNullOrWhiteSpace(m.Detail)
                    ? $"[{m.Identifier}] {m.Text}"
                    : $"[{m.Identifier}] {m.Text} — {m.Detail}"));
        return $"{prefix} {detail}";
    }

    private static ClaveAcceso BuildAccessKey(ElectronicDocument invoice)
    {
        var random = Random.Shared.Next(10000000, 99999999);
        return ClaveAcceso.Create(new ClaveAccesoComponents(
            invoice.IssueDate,
            invoice.DocumentType,
            invoice.EmitterRuc,
            "1",
            invoice.Establishment,
            invoice.EmissionPoint,
            invoice.Sequential,
            random,
            "1"));
    }

    private byte[] BuildDocumentXml(
        SalesDocument document,
        InvoiceXmlEmitterContext emitterCtx,
        ClaveAcceso accessKey) =>
        document switch
        {
            ElectronicCreditNote note => creditNoteXmlGenerator.BuildXml(note, emitterCtx, accessKey),
            ElectronicInvoice invoice => xmlGenerator.BuildXml(invoice, emitterCtx, accessKey),
            _ => throw new InvalidOperationException("Tipo de comprobante no soportado."),
        };

    private static ElectronicDocumentSchema SchemaOf(SalesDocument document) =>
        document.DocumentType.Value == DocumentTypeCode.NotaCredito.Value
            ? ElectronicDocumentSchema.NotaCreditoV110
            : ElectronicDocumentSchema.FacturaV110;

    private static string SchemaName(SalesDocument document) =>
        document.DocumentType.Value == DocumentTypeCode.NotaCredito.Value
            ? "Nota de Crédito v1.1.0"
            : "Factura v1.1.0";

    private static DateOnly EcuadorToday() =>
        DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));

    private static bool IsConsumerFinal(string? identificationType) =>
        string.Equals(
            identificationType,
            TaxRuleCodes.ConsumidorFinalIdType,
            StringComparison.Ordinal);
}
