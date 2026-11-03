using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Sri;

namespace Ecunexo.Billing.Infrastructure.Xml;

/// <summary>
/// Envuelve un comprobante electrónico firmado (&lt;factura&gt;, &lt;notaCredito&gt;, etc.) en el contenedor
/// oficial &lt;autorizacion&gt; exigido por la Ficha Técnica del SRI (Esquema Offline) para entrega al receptor.
/// </summary>
public static class SriAuthorizationXmlEnvelope
{
    private static readonly TimeSpan EcuadorUtcOffset = TimeSpan.FromHours(-5);

    public static string WrapSignedComprobante(
        string signedComprobanteXml,
        string? accessKey,
        DateTimeOffset? authorizationDate,
        SriEnvironment environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signedComprobanteXml);

        var trimmed = signedComprobanteXml.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        try
        {
            var existingDoc = XDocument.Parse(trimmed, LoadOptions.PreserveWhitespace);
            if (existingDoc.Root is not null &&
                string.Equals(existingDoc.Root.Name.LocalName, "autorizacion", StringComparison.Ordinal))
            {
                return SerializeWithUtf8Declaration(existingDoc);
            }
        }
        catch
        {
            // Si no se puede parsear como XDocument completo, se envuelve igualmente en CDATA.
        }

        var numeroAutorizacion = !string.IsNullOrWhiteSpace(accessKey)
            ? accessKey.Trim()
            : string.Empty;

        var ambiente = ResolveAmbienteLabel(numeroAutorizacion, environment);
        var ecuadorAuthTime = (authorizationDate ?? DateTimeOffset.UtcNow).ToOffset(EcuadorUtcOffset);
        var fechaAutorizacion = ecuadorAuthTime.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

        var autorizacion = new XElement(
            "autorizacion",
            new XElement("estado", "AUTORIZADO"),
            new XElement("numeroAutorizacion", numeroAutorizacion),
            new XElement(
                "fechaAutorizacion",
                new XAttribute("class", "fechaAutorizacion"),
                fechaAutorizacion),
            new XElement("ambiente", ambiente),
            new XElement("comprobante", new XCData(trimmed)),
            new XElement("mensajes"));

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), autorizacion);
        return SerializeWithUtf8Declaration(doc);
    }

    private static string ResolveAmbienteLabel(string accessKey, SriEnvironment environment)
    {
        if (accessKey.Length >= 24)
        {
            if (accessKey[23] == '2')
                return "PRODUCCION";
            if (accessKey[23] == '1')
                return "PRUEBAS";
        }

        return environment == SriEnvironment.Production ? "PRODUCCION" : "PRUEBAS";
    }

    private static string SerializeWithUtf8Declaration(XDocument doc)
    {
        using var ms = new MemoryStream();
        using (var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
        {
            doc.Save(writer, SaveOptions.DisableFormatting);
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
