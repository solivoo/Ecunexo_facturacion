using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecunexo.Billing.Infrastructure.Email;

public sealed class HttpInvoiceEmailNotifier(
    IHttpClientFactory httpClientFactory,
    IOptions<InvoiceEmailOptions> options,
    ILogger<HttpInvoiceEmailNotifier> logger) : IInvoiceEmailNotifier
{
    public const string HttpClientName = "InvoiceEmail";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task NotifyAuthorizedAsync(
        Guid tenantId,
        InvoiceEmailNotifyRequest request,
        CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        if (!opts.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(opts.BaseUrl) || string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            logger.LogWarning(
                "InvoiceEmail habilitado pero BaseUrl/ApiKey incompletos; se omite correo de {InvoiceId}",
                request.BillingInvoiceId);
            return;
        }

        if (tenantId == Guid.Empty)
        {
            logger.LogWarning(
                "Factura {InvoiceId} autorizada sin TenantId; no se notifica correo",
                request.BillingInvoiceId);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.CounterpartyEmail))
        {
            logger.LogInformation(
                "Factura {InvoiceId} sin correo de contraparte; se omite notificación",
                request.BillingInvoiceId);
            return;
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        var url = $"api/v1/tenants/{tenantId:D}/billing/invoice-authorized-email";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };
        httpRequest.Headers.TryAddWithoutValidation("X-EcuNexo-Billing-Key", opts.ApiKey);

        try
        {
            using var response = await client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                logger.LogWarning(
                    "Correo de factura falló para {InvoiceId}: {Status} {Body}",
                    request.BillingInvoiceId,
                    (int)response.StatusCode,
                    body);
                return;
            }

            logger.LogInformation(
                "Correo de factura autorizada enviado para {InvoiceId} tenant {TenantId} a {Email}",
                request.BillingInvoiceId,
                tenantId,
                request.CounterpartyEmail);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // No tumba el outbox SRI: el correo es best-effort.
            logger.LogError(ex, "Error notificando correo de factura {InvoiceId}", request.BillingInvoiceId);
        }
    }
}
