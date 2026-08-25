using System.Text;
using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Sri;
using Ecunexo.Billing.Domain.Sri.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecunexo.Billing.Infrastructure.Sri.Adapters;

/// <summary>
/// Cliente SOAP offline del SRI (recepción + autorización) vía HTTP POST.
/// </summary>
public sealed class SoapSriGateway : ISriGateway
{
    public const string HttpClientName = "SriSoap";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SriOptions _options;
    private readonly ILogger<SoapSriGateway> _logger;

    public SoapSriGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<SriOptions> options,
        ILogger<SoapSriGateway> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SriTransmissionResult> SendReceptionAsync(
        SriEnvironment environment,
        byte[] signedXml,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signedXml);
        if (signedXml.Length == 0)
            throw new ArgumentException("El XML firmado no puede estar vacío.");

        var endpoints = _options.ResolveEndpoints(environment);
        var fallbackKey = SriSoapResponseMapper.TryExtractAccessKeyFromSignedXml(signedXml);
        var envelope = SriSoapResponseMapper.BuildReceptionEnvelope(signedXml);

        try
        {
            var responseXml = await PostSoapAsync(
                endpoints.ReceptionUrl,
                envelope,
                cancellationToken).ConfigureAwait(false);
            return SriSoapResponseMapper.MapReception(responseXml, fallbackKey);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error de transporte en recepción SRI ({Environment})", environment);
            return SriTransmissionResult.Create(
                fallbackKey,
                SriTransmissionState.TransportError,
                [
                    SriMessage.Create(
                        "SRI_TRANSPORT",
                        "Fallo de transporte al WS de recepción SRI.",
                        SriMessageType.Error,
                        ex.Message),
                ]);
        }
    }

    public async Task<SriTransmissionResult> QueryAuthorizationAsync(
        SriEnvironment environment,
        ClaveAcceso accessKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accessKey);
        var endpoints = _options.ResolveEndpoints(environment);
        var envelope = SriSoapResponseMapper.BuildAuthorizationEnvelope(accessKey.Value);

        try
        {
            var responseXml = await PostSoapAsync(
                endpoints.AuthorizationUrl,
                envelope,
                cancellationToken).ConfigureAwait(false);
            return SriSoapResponseMapper.MapAuthorization(responseXml, accessKey.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error de transporte en autorización SRI ({Environment})", environment);
            return SriTransmissionResult.Create(
                accessKey.Value,
                SriTransmissionState.TransportError,
                [
                    SriMessage.Create(
                        "SRI_TRANSPORT",
                        "Fallo de transporte al WS de autorización SRI.",
                        SriMessageType.Error,
                        ex.Message),
                ]);
        }
    }

    public Task<SriTransmissionResult> QueryValidityStatusAsync(
        SriEnvironment environment,
        ClaveAcceso accessKey,
        CancellationToken cancellationToken = default) =>
        QueryAuthorizationAsync(environment, accessKey, cancellationToken);

    private async Task<string> PostSoapAsync(
        string url,
        string envelope,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("URL del WS SRI no configurada.");

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        content.Headers.ContentType!.CharSet = "utf-8";

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.TryAddWithoutValidation("SOAPAction", "\"\"");

        _logger.LogInformation("SRI SOAP POST {Url}", url);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "SRI SOAP HTTP {Status}: {Body}",
                (int)response.StatusCode,
                Truncate(body, 500));
            throw new HttpRequestException(
                $"SRI respondió HTTP {(int)response.StatusCode}.");
        }

        _logger.LogInformation("SRI SOAP HTTP 200 ({Length} chars): {Body}", body.Length, Truncate(body, 800));
        return body;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
