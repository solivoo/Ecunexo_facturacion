using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecunexo.Billing.Infrastructure.Inventory;

public sealed class HttpInventoryEgressNotifier(
    IHttpClientFactory httpClientFactory,
    IOptions<InventoryEgressOptions> options,
    ILogger<HttpInventoryEgressNotifier> logger) : IInventoryEgressNotifier
{
    public const string HttpClientName = "InventoryEgress";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task NotifyAuthorizedAsync(
        Guid tenantId,
        Guid billingInvoiceId,
        IReadOnlyList<InventoryEgressLineDto> lines,
        CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        if (!opts.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(opts.BaseUrl) || string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            logger.LogWarning(
                "InventoryEgress habilitado pero BaseUrl/ApiKey incompletos; se omite egreso de {InvoiceId}",
                billingInvoiceId);
            return;
        }

        if (tenantId == Guid.Empty)
        {
            logger.LogWarning(
                "Factura {InvoiceId} autorizada sin TenantId; no se notifica egreso",
                billingInvoiceId);
            return;
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        var url = $"api/v1/tenants/{tenantId:D}/inventory/billing-egress";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(
                new InventoryEgressRequestDto(billingInvoiceId, lines),
                options: JsonOptions),
        };
        request.Headers.TryAddWithoutValidation("X-EcuNexo-Inventory-Key", opts.ApiKey);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                logger.LogWarning(
                    "Egreso inventario falló para factura {InvoiceId}: {Status} {Body}",
                    billingInvoiceId,
                    (int)response.StatusCode,
                    body);
                return;
            }

            logger.LogInformation(
                "Egreso inventario notificado para factura {InvoiceId} tenant {TenantId}",
                billingInvoiceId,
                tenantId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // No tumba el outbox SRI: el egreso se puede reintentar luego.
            logger.LogError(ex, "Error notificando egreso de factura {InvoiceId}", billingInvoiceId);
        }
    }
}
