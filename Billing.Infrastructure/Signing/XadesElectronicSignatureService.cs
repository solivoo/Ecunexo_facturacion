using System.Text;
using Ecunexo.Billing.Domain.Emitter.Ports;
using Microsoft.Extensions.Logging;
using Yamgooo.SRI.Sign;

namespace Ecunexo.Billing.Infrastructure.Signing;

/// <summary>
/// Firma XML con XAdES-BES (enveloped) usando el PKCS#12 de Infisical vía Yamgooo.SRI.Sign.
/// </summary>
public sealed class XadesElectronicSignatureService : IElectronicSignatureService
{
    private readonly ISigningPkcs12MaterialProvider _pkcs12;
    private readonly ISriSignService _sriSign;
    private readonly ILogger<XadesElectronicSignatureService> _logger;

    public XadesElectronicSignatureService(
        ISigningPkcs12MaterialProvider pkcs12,
        ISriSignService sriSign,
        ILogger<XadesElectronicSignatureService> logger)
    {
        _pkcs12 = pkcs12;
        _sriSign = sriSign;
        _logger = logger;
    }

    public async Task<byte[]> SignXmlAsync(
        Guid emitterId,
        byte[] xml,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(xml);

        if (emitterId == Guid.Empty)
            throw new ArgumentException("El identificador del emisor es obligatorio.");

        if (xml.Length == 0)
            throw new ArgumentException("El XML a firmar está vacío.", nameof(xml));

        cancellationToken.ThrowIfCancellationRequested();

        var material = await _pkcs12.GetPkcs12Async(cancellationToken).ConfigureAwait(false);
        var xmlText = Encoding.UTF8.GetString(xml);

        _logger.LogInformation("Firmando XML XAdES-BES para emisor {EmitterId} ({Bytes} bytes)", emitterId, xml.Length);

        var result = await _sriSign
            .SignWithBase64CertificateAsync(xmlText, material.ToBase64(), material.Password)
            .ConfigureAwait(false);

        if (!result.Success || string.IsNullOrWhiteSpace(result.SignedXml))
        {
            _logger.LogError("Firma XAdES falló: {Error}", result.ErrorMessage);
            throw new InvalidOperationException(
                result.ErrorMessage ?? "No se pudo firmar el XML con XAdES-BES.");
        }

        _logger.LogInformation(
            "XML firmado OK en {Ms} ms (emisor {EmitterId})",
            result.ProcessingTimeMs,
            emitterId);

        return Encoding.UTF8.GetBytes(result.SignedXml);
    }
}
