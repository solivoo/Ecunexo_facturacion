using System.Net;
using System.Net.Http.Json;
using Ecunexo.Billing.Api.Contracts.Emitters;
using Ecunexo.Billing.Api.Contracts.Invoices;
using Ecunexo.Billing.Api.Tests.Support;
using Xunit;

namespace Ecunexo.Billing.Api.Tests.Endpoints;

public sealed class EndpointsSmokeTests : IClassFixture<BillingApiFactory>
{
    private readonly HttpClient _client;

    public EndpointsSmokeTests(BillingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "GET tax-rates responde OK")]
    public async Task GetTaxRates_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/catalogs/tax-rates?taxCode=2&date=2024-01-21");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "GET tax-rules responde OK")]
    public async Task GetTaxRules_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/catalogs/tax-rules?code=sri.void.online");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "Flujo básico create/sign/submit/poll/status funciona")]
    public async Task BasicOfflineFlow_Works()
    {
        var emitterCreate = await _client.PostAsJsonAsync("/api/v1/emitters", new CreateEmitterRequest(
            "1792146739001",
            "Empresa Demo",
            "Quito",
            "Demo"));

        Assert.Equal(HttpStatusCode.Created, emitterCreate.StatusCode);
        var emitter = await emitterCreate.Content.ReadFromJsonAsync<CreateEmitterResponse>();
        Assert.NotNull(emitter);

        var emitterAgain = await _client.PostAsJsonAsync("/api/v1/emitters", new CreateEmitterRequest(
            "1792146739001",
            "Empresa Demo",
            "Quito",
            "Demo"));
        Assert.Equal(HttpStatusCode.OK, emitterAgain.StatusCode);
        var sameEmitter = await emitterAgain.Content.ReadFromJsonAsync<CreateEmitterResponse>();
        Assert.NotNull(sameEmitter);
        Assert.Equal(emitter!.EmitterId, sameEmitter!.EmitterId);

        var certResponse = await _client.PostAsJsonAsync(
            $"/api/v1/emitters/{emitter!.EmitterId}/certificates",
            new AssignCertificateRequest(
                "SER-001",
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30),
                "KeyVault",
                "cert-demo",
                "KeyVault"));

        Assert.Equal(HttpStatusCode.OK, certResponse.StatusCode);

        var invoiceRequest = new CreateInvoiceRequest(
            "1792146739001",
            "001",
            "001",
            "000000001",
            new DateOnly(2024, 1, 21),
            new CounterpartyRequest("04", "0999999999001", "Cliente Test", null),
            [new InvoiceLineRequest(
                1,
                "Servicio",
                1m,
                100m,
                0m,
                100m,
                [new LineTaxRequest("2", "4", 15m, 100m, 15m)])]);

        var createInvoice = await _client.PostAsJsonAsync($"/api/v1/emitters/{emitter.EmitterId}/invoices", invoiceRequest);
        Assert.Equal(HttpStatusCode.Created, createInvoice.StatusCode);

        var invoice = await createInvoice.Content.ReadFromJsonAsync<CreateInvoiceResponse>();
        Assert.NotNull(invoice);

        var sign = await _client.PostAsync($"/api/v1/emitters/{emitter.EmitterId}/invoices/{invoice!.InvoiceId}/sign", null);
        Assert.Equal(HttpStatusCode.OK, sign.StatusCode);

        var submit = await _client.PostAsync($"/api/v1/emitters/{emitter.EmitterId}/invoices/{invoice.InvoiceId}/submit-reception", null);
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        var poll = await _client.PostAsync($"/api/v1/emitters/{emitter.EmitterId}/invoices/{invoice.InvoiceId}/authorize-poll", null);
        Assert.Equal(HttpStatusCode.OK, poll.StatusCode);

        var status = await _client.GetAsync($"/api/v1/emitters/{emitter.EmitterId}/invoices/{invoice.InvoiceId}/sri-status");
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);

        var state = await status.Content.ReadFromJsonAsync<InvoiceStateResponse>();
        Assert.NotNull(state);
        Assert.NotNull(state!.AccessKey);

        var creditNoteCreate = await _client.PostAsJsonAsync(
            $"/api/v1/emitters/{emitter.EmitterId}/invoices/{invoice.InvoiceId}/credit-notes",
            new CreateCreditNoteRequest("Anulación total de prueba"));
        Assert.Equal(HttpStatusCode.Created, creditNoteCreate.StatusCode);

        var creditNote = await creditNoteCreate.Content.ReadFromJsonAsync<CreateCreditNoteResponse>();
        Assert.NotNull(creditNote);
        Assert.Equal(invoice.InvoiceId, creditNote!.InvoiceId);

        var previewNc = await _client.PostAsync(
            $"/api/v1/emitters/{emitter.EmitterId}/invoices/{creditNote.CreditNoteId}/preview-xml",
            null);
        Assert.Equal(HttpStatusCode.OK, previewNc.StatusCode);
    }

    [Fact(DisplayName = "GET ride-provider responde OK")]
    public async Task GetRideProvider_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/ride-provider");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
