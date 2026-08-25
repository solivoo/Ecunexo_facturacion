using System.Net;
using Ecunexo.Billing.Api.Tests.Support;

namespace Ecunexo.Billing.Api.Tests.Endpoints;

public sealed class BillingModuleEndpointsTests : IClassFixture<BillingApiFactory>
{
    private readonly HttpClient _client;

    public BillingModuleEndpointsTests(BillingApiFactory factory) =>
        _client = factory.CreateClient();

    [Fact(DisplayName = "GET billing/menu responde MenuConfig")]
    public async Task GetMenu_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/billing/menu");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("facturacion", body, StringComparison.Ordinal);
        Assert.Contains("Emitir", body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "GET billing/menu filtra por permisos")]
    public async Task GetMenu_FiltersByPermissions()
    {
        var response = await _client.GetAsync(
            "/api/v1/billing/menu?permissions=facturacion:read,facturacion:facturas:create");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Emitir", body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "GET billing/permissions lista catálogo MVP")]
    public async Task GetPermissions_MvpOnly_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/billing/permissions?mvpOnly=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
