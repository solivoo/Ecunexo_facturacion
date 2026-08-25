using System.Text;
using System.Xml.Linq;
using Ecunexo.Billing.Domain.Sri;

namespace Ecunexo.Billing.Infrastructure.Sri;

/// <summary>Mapea XML de respuesta SOAP SRI offline a <see cref="SriTransmissionResult"/>.</summary>
public static class SriSoapResponseMapper
{
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";

    public static SriTransmissionResult MapReception(string soapXml, string fallbackAccessKey)
    {
        var doc = XDocument.Parse(soapXml);
        var body = doc.Root?.Element(Soap + "Body") ?? doc.Root;
        var fault = FindLocal(body, "Fault") ?? FindLocal(body, "fault");
        if (fault is not null)
        {
            var faultText =
                FindLocal(fault, "faultstring")?.Value?.Trim()
                ?? FindLocal(fault, "Reason")?.Value?.Trim()
                ?? "SOAP Fault en recepción SRI.";
            return SriTransmissionResult.Create(
                NormalizeAccessKey(null, fallbackAccessKey),
                SriTransmissionState.TransportError,
                [SriMessage.Create("SRI_SOAP_FAULT", faultText, SriMessageType.Error)]);
        }

        var respuesta = FindLocal(body, "RespuestaRecepcionComprobante") ?? body;
        var estado = FindDirectChild(respuesta, "estado")?.Value?.Trim()
            ?? FindLocal(respuesta, "estado")?.Value?.Trim()
            ?? string.Empty;
        var messages = ParseMessages(respuesta);
        var clave = FindLocal(respuesta, "claveAcceso")?.Value?.Trim();
        var accessKey = NormalizeAccessKey(clave, fallbackAccessKey);

        var state = estado.ToUpperInvariant() switch
        {
            "RECIBIDA" => SriTransmissionState.Received,
            "DEVUELTA" => SriTransmissionState.Returned,
            _ => SriTransmissionState.TransportError,
        };

        if (state is SriTransmissionState.TransportError && messages.Count == 0)
        {
            messages =
            [
                SriMessage.Create(
                    "SRI_RECEPTION_UNKNOWN",
                    string.IsNullOrWhiteSpace(estado)
                        ? "Respuesta de recepción SRI sin estado reconocible."
                        : $"Estado de recepción no mapeado: {estado}",
                    SriMessageType.Error),
            ];
        }

        if (state is SriTransmissionState.Returned && messages.Count == 0)
        {
            messages =
            [
                SriMessage.Create(
                    "SRI_DEVUELTA",
                    "El SRI devolvió el comprobante (DEVUELTA) sin detalle de mensajes.",
                    SriMessageType.Error),
            ];
        }

        return SriTransmissionResult.Create(accessKey, state, messages);
    }

    public static SriTransmissionResult MapAuthorization(string soapXml, string accessKey)
    {
        var doc = XDocument.Parse(soapXml);
        var body = doc.Root?.Element(Soap + "Body") ?? doc.Root;
        var fault = FindLocal(body, "Fault") ?? FindLocal(body, "fault");
        if (fault is not null)
        {
            var faultText =
                FindLocal(fault, "faultstring")?.Value?.Trim()
                ?? FindLocal(fault, "Reason")?.Value?.Trim()
                ?? "SOAP Fault en autorización SRI.";
            return SriTransmissionResult.Create(
                NormalizeAccessKey(null, accessKey),
                SriTransmissionState.TransportError,
                [SriMessage.Create("SRI_SOAP_FAULT", faultText, SriMessageType.Error)]);
        }

        var respuesta = FindLocal(body, "RespuestaAutorizacionComprobante") ?? body;
        var autorizacion = FindLocal(respuesta, "autorizacion");
        var messages = ParseMessages(respuesta);
        var numeroComprobantes = FindDirectChild(respuesta, "numeroComprobantes")?.Value?.Trim();

        var estadoRaw =
            FindDirectChild(autorizacion, "estado")?.Value?.Trim()
            ?? FindLocal(autorizacion, "estado")?.Value?.Trim()
            ?? string.Empty;

        var estado = estadoRaw.ToUpperInvariant();
        var key = NormalizeAccessKey(
            FindLocal(autorizacion, "numeroAutorizacion")?.Value
                ?? FindLocal(autorizacion, "claveAcceso")?.Value
                ?? FindLocal(respuesta, "claveAccesoConsultada")?.Value,
            accessKey);

        if (estado is "AUTORIZADO" or "AUT")
        {
            var authorizedXml =
                FindLocal(autorizacion, "comprobante")?.Value
                ?? autorizacion?.ToString(SaveOptions.DisableFormatting)
                ?? soapXml;
            var fechaRaw = FindLocal(autorizacion, "fechaAutorizacion")?.Value;
            var fecha = ParseSriDate(fechaRaw) ?? DateTimeOffset.UtcNow;
            return SriTransmissionResult.Create(
                key,
                SriTransmissionState.Authorized,
                messages,
                authorizedXml,
                fecha);
        }

        if (estado is "NO AUTORIZADO" or "NOAUTORIZADO" or "NAT")
            return SriTransmissionResult.Create(key, SriTransmissionState.NotAuthorized, messages);

        if (estado is "EN PROCESAMIENTO" or "ENPROCESAMIENTO" or "PPR")
            return SriTransmissionResult.Create(key, SriTransmissionState.Processing, messages);

        // SRI responde <autorizaciones/> y numeroComprobantes=0 cuando aún no hay resultado.
        if (autorizacion is null
            && (numeroComprobantes is "0" || string.IsNullOrWhiteSpace(estadoRaw)))
        {
            return SriTransmissionResult.Create(
                key,
                SriTransmissionState.Processing,
                messages.Count > 0
                    ? messages
                    :
                    [
                        SriMessage.Create(
                            "SRI_AUTH_EMPTY",
                            "El SRI aún no devolvió autorizaciones para esta clave (reintentar en unos segundos).",
                            SriMessageType.Warning),
                    ]);
        }

        if (messages.Count > 0)
            return SriTransmissionResult.Create(key, SriTransmissionState.Processing, messages);

        return SriTransmissionResult.Create(
            key,
            SriTransmissionState.TransportError,
            [
                SriMessage.Create(
                    "SRI_AUTH_UNKNOWN",
                    string.IsNullOrWhiteSpace(estadoRaw)
                        ? "Respuesta de autorización SRI sin estado reconocible."
                        : $"Estado de autorización no mapeado: {estadoRaw}",
                    SriMessageType.Error),
            ]);
    }

    public static string BuildReceptionEnvelope(byte[] signedXml)
    {
        var b64 = Convert.ToBase64String(signedXml);
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ec="http://ec.gob.sri.ws.recepcion">
              <soapenv:Header/>
              <soapenv:Body>
                <ec:validarComprobante>
                  <xml>{b64}</xml>
                </ec:validarComprobante>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
    }

    public static string BuildAuthorizationEnvelope(string accessKey) =>
        $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ec="http://ec.gob.sri.ws.autorizacion">
          <soapenv:Header/>
          <soapenv:Body>
            <ec:autorizacionComprobante>
              <claveAccesoComprobante>{accessKey}</claveAccesoComprobante>
            </ec:autorizacionComprobante>
          </soapenv:Body>
        </soapenv:Envelope>
        """;

    private static IReadOnlyList<SriMessage> ParseMessages(XElement? root)
    {
        if (root is null) return [];

        var list = new List<SriMessage>();
        // El SRI anida <mensaje>texto</mensaje> dentro de <mensaje>…</mensaje>;
        // solo tomamos nodos que traen <identificador> (bloque de mensaje real).
        foreach (var msg in root.Descendants().Where(IsSriMessageBlock))
        {
            var id = FindDirectChild(msg, "identificador")?.Value?.Trim() ?? "SRI";
            var text = FindDirectChild(msg, "mensaje")?.Value?.Trim()
                ?? FindDirectChild(msg, "informacionAdicional")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var tipoRaw = FindDirectChild(msg, "tipo")?.Value?.Trim() ?? "ERROR";
            var type = tipoRaw.Contains("ADVERTENCIA", StringComparison.OrdinalIgnoreCase)
                || tipoRaw.Contains("WARNING", StringComparison.OrdinalIgnoreCase)
                ? SriMessageType.Warning
                : SriMessageType.Error;
            var detail = FindDirectChild(msg, "informacionAdicional")?.Value?.Trim();
            list.Add(SriMessage.Create(id, text, type, detail));
        }

        return list;
    }

    private static bool IsSriMessageBlock(XElement e) =>
        e.Name.LocalName == "mensaje"
        && e.Elements().Any(c => c.Name.LocalName == "identificador");

    private static XElement? FindDirectChild(XElement? root, string localName) =>
        root?.Elements().FirstOrDefault(e => e.Name.LocalName == localName);

    private static XElement? FindLocal(XElement? root, string localName) =>
        root?.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);

    private static string NormalizeAccessKey(string? candidate, string fallback)
    {
        var value = candidate?.Trim();
        if (!string.IsNullOrWhiteSpace(value) && value.Length == 49 && value.All(char.IsDigit))
            return value;
        if (fallback.Length == 49 && fallback.All(char.IsDigit))
            return fallback;
        return new string('0', 49);
    }

    private static DateTimeOffset? ParseSriDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        // Npgsql timestamptz solo acepta offset 0 (UTC).
        if (DateTimeOffset.TryParse(raw, out var dto))
            return dto.ToUniversalTime();
        if (DateTime.TryParse(raw, out var dt))
            return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
        return null;
    }

    public static string TryExtractAccessKeyFromSignedXml(byte[] signedXml)
    {
        var text = Encoding.UTF8.GetString(signedXml);
        const string marker = "claveAcceso";
        var idx = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return new string('0', 49);

        var after = text[idx..];
        var digitStart = -1;
        for (var i = 0; i < after.Length; i++)
        {
            if (char.IsDigit(after[i]))
            {
                digitStart = i;
                break;
            }
        }

        if (digitStart < 0) return new string('0', 49);
        var digits = new string(after[digitStart..].TakeWhile(char.IsDigit).ToArray());
        return digits.Length == 49 ? digits : new string('0', 49);
    }
}
