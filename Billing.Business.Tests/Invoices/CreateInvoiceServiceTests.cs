using Ecunexo.Billing.Business.Invoices;
using Ecunexo.Billing.Business.Tests.Support;
using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents;
using Ecunexo.Billing.Core.Emitter;
using Ecunexo.Billing.Core.Sri;
using Ecunexo.Billing.Core.TaxCatalog.Ports;

namespace Ecunexo.Billing.Business.Tests.Invoices;

public sealed class CreateInvoiceServiceTests
{
    [Fact(DisplayName = "CreateInvoice: emisor inexistente devuelve NotFound")]
    public async Task ExecuteAsync_WhenEmitterMissing_ReturnsNotFound()
    {
        var service = CreateService(out _, out _, out _);
        var command = ValidCommand(Guid.NewGuid());

        var result = await service.ExecuteAsync(command);

        var notFound = Assert.IsType<CreateInvoiceNotFound>(result);
        Assert.Contains("Emisor no encontrado", notFound.Message);
    }

    [Fact(DisplayName = "CreateInvoice: comando válido crea factura en borrador y persiste")]
    public async Task ExecuteAsync_WithValidCommand_ReturnsSuccessAndPersists()
    {
        var emitter = CreateEmitter();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService(out var invoiceRepository, out var emitterRepository, out _);
        emitterRepository.Add(emitter);

        var command = ValidCommand(emitter.Id) with
        {
            TenantId = tenantId,
            CreatedByUserId = userId
        };

        var result = await service.ExecuteAsync(command);

        var success = Assert.IsType<CreateInvoiceSuccess>(result);
        Assert.Equal(SriDocumentState.Draft, success.Invoice.State);
        Assert.Equal("01", success.Invoice.DocumentType.Value);
        Assert.Equal(115m, success.Invoice.GrandTotal.Amount);
        Assert.Single(invoiceRepository.Saved);
        Assert.Equal(emitter.Id, invoiceRepository.Saved[0].EmitterId);
        Assert.Equal(tenantId, invoiceRepository.Saved[0].TenantId);
        Assert.Equal(userId, invoiceRepository.Saved[0].CreatedByUserId);
    }

    [Fact(DisplayName = "CreateInvoice: sin líneas devuelve BadRequest")]
    public async Task ExecuteAsync_WithNoLines_ReturnsBadRequest()
    {
        var emitter = CreateEmitter();
        var service = CreateService(out _, out var emitterRepository, out _);
        emitterRepository.Add(emitter);

        var command = ValidCommand(emitter.Id) with { Lines = [] };

        var result = await service.ExecuteAsync(command);

        var badRequest = Assert.IsType<CreateInvoiceBadRequest>(result);
        Assert.Contains("al menos una línea", badRequest.Message);
    }

    [Fact(DisplayName = "CreateInvoice: contraparte inválida devuelve BadRequest")]
    public async Task ExecuteAsync_WithInvalidCounterparty_ReturnsBadRequest()
    {
        var emitter = CreateEmitter();
        var service = CreateService(out _, out var emitterRepository, out _);
        emitterRepository.Add(emitter);

        var command = ValidCommand(emitter.Id) with
        {
            Counterparty = new CreateInvoiceCounterparty("04", "", "Cliente", null, null, null)
        };

        var result = await service.ExecuteAsync(command);

        Assert.IsType<CreateInvoiceBadRequest>(result);
    }

    [Fact(DisplayName = "CreateInvoice: punto no registrado devuelve Conflict")]
    public async Task ExecuteAsync_WhenPointResolutionFails_ReturnsConflict()
    {
        var emitter = CreateEmitter();
        var service = CreateService(out _, out var emitterRepository, out _);
        emitterRepository.Add(emitter);
        emitterRepository.ResolveException = new InvalidOperationException("Punto de emisión no registrado.");

        var result = await service.ExecuteAsync(ValidCommand(emitter.Id));

        var conflict = Assert.IsType<CreateInvoiceConflict>(result);
        Assert.Equal("Punto de emisión no registrado.", conflict.Message);
    }

    [Fact(DisplayName = "CreateInvoice: identidad sustituida registra estab/pto del certificado")]
    public async Task ExecuteAsync_WithSubstitutedIdentity_EnsuresCertificatePoint()
    {
        var emitter = CreateEmitter();
        var enrolled = Emitter.Create(
            Ruc.Create("0926398074001"),
            "Ecunexo S.A",
            "Guayaquil via Daule");
        var service = CreateService(out _, out var emitterRepository, out var identityResolver);
        emitterRepository.Add(emitter);
        emitterRepository.Add(enrolled);

        identityResolver.ResolveOverride = (_, _, _) =>
            new SriEmissionIdentity(
                Ruc.Create("0926398074001"),
                "Ecunexo S.A",
                "Guayaquil via Daule",
                null,
                EstablishmentCode.Create("002"),
                EmissionPoint.Create("001"),
                IsSubstituted: true);

        var result = await service.ExecuteAsync(ValidCommand(emitter.Id));

        Assert.IsType<CreateInvoiceSuccess>(result);
        Assert.Contains(
            emitterRepository.EnsuredPoints,
            p => p.Establishment == "002" && p.EmissionPoint == "001" && p.DocumentType == "01");
    }

    private static CreateInvoiceService CreateService(
        out FakeInvoiceRepository invoiceRepository,
        out FakeEmitterRepository emitterRepository,
        out PassthroughEmissionIdentityResolver identityResolver)
    {
        ITaxRateRepository taxRates = TestTaxRateCatalog.WithIva15();
        invoiceRepository = new FakeInvoiceRepository();
        emitterRepository = new FakeEmitterRepository();
        identityResolver = new PassthroughEmissionIdentityResolver();
        return new CreateInvoiceService(taxRates, emitterRepository, invoiceRepository, identityResolver);
    }

    private static Emitter CreateEmitter() =>
        Emitter.Create(
            Ruc.Create("1792146739001"),
            "Empresa Prueba",
            "Av. Principal 123");

    private static CreateInvoiceCommand ValidCommand(Guid emitterId) =>
        new(
            emitterId,
            TenantId: null,
            CreatedByUserId: null,
            Establishment: "001",
            EmissionPoint: "001",
            IssueDate: new DateOnly(2024, 1, 21),
            new CreateInvoiceCounterparty("04", "0999999999001", "Cliente Prueba", "Quito", null, null),
            [
                new CreateInvoiceLine(
                    1,
                    "Servicio",
                    2m,
                    50m,
                    0m,
                    100m,
                    [new CreateInvoiceLineTax("2", "4", 15m, 100m, 15m)],
                    null,
                    null,
                    null)
            ],
            PaymentFormCode: null,
            AdditionalNote: null,
            PaymentTermDays: 0);
}
