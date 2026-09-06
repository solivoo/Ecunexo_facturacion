using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Sri;
using Ecunexo.Billing.Core.Sri.Ports;

namespace Ecunexo.Billing.Infrastructure.Sri.Adapters;

/// <summary>Adaptador en memoria para pruebas y desarrollo sin SOAP real.</summary>
public sealed class InMemorySriGateway : ISriGateway
{
    private readonly Func<byte[], SriTransmissionResult>? _receptionHandler;
    private readonly Func<ClaveAcceso, SriTransmissionResult>? _authorizationHandler;

    public InMemorySriGateway(
        Func<byte[], SriTransmissionResult>? receptionHandler = null,
        Func<ClaveAcceso, SriTransmissionResult>? authorizationHandler = null)
    {
        _receptionHandler = receptionHandler;
        _authorizationHandler = authorizationHandler;
    }

    public Task<SriTransmissionResult> SendReceptionAsync(
        SriEnvironment environment,
        byte[] signedXml,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signedXml);

        if (signedXml.Length == 0)
            throw new ArgumentException("El XML firmado no puede estar vacío.");

        var result = _receptionHandler?.Invoke(signedXml)
            ?? SriTransmissionResult.Create(
                ExtractAccessKeyPlaceholder(signedXml),
                SriTransmissionState.Received);

        return Task.FromResult(result);
    }

    public Task<SriTransmissionResult> QueryAuthorizationAsync(
        SriEnvironment environment,
        ClaveAcceso accessKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accessKey);

        var result = _authorizationHandler?.Invoke(accessKey)
            ?? SriTransmissionResult.Create(
                accessKey.Value,
                SriTransmissionState.Authorized,
                authorizedXml: "<autorizacion ambiente=\"1\">stub-dev</autorizacion>",
                authorizationDate: DateTimeOffset.UtcNow);



        return Task.FromResult(result);
    }

    public Task<SriTransmissionResult> QueryValidityStatusAsync(
        SriEnvironment environment,
        ClaveAcceso accessKey,
        CancellationToken cancellationToken = default) =>
        QueryAuthorizationAsync(environment, accessKey, cancellationToken);

    private static string ExtractAccessKeyPlaceholder(byte[] signedXml)
    {
        var text = System.Text.Encoding.UTF8.GetString(signedXml);
        const string marker = "claveAcceso=\"";
        var start = text.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            return new string('0', 49);

        start += marker.Length;
        var end = text.IndexOf('"', start);
        if (end <= start)
            return new string('0', 49);

        var key = text[start..end];
        return key.Length == 49 ? key : new string('0', 49);
    }
}
