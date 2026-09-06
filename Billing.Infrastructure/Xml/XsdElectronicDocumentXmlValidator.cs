using System.Xml;
using System.Xml.Schema;
using Ecunexo.Billing.Core.Emitter.Ports;
using Microsoft.Extensions.Logging;

namespace Ecunexo.Billing.Infrastructure.Xml;

public sealed class XsdElectronicDocumentXmlValidator : IElectronicDocumentXmlValidator
{
    private readonly ILogger<XsdElectronicDocumentXmlValidator> _logger;
    private readonly Lazy<XmlSchemaSet> _facturaSchemas;
    private readonly Lazy<XmlSchemaSet> _notaCreditoSchemas;

    public XsdElectronicDocumentXmlValidator(ILogger<XsdElectronicDocumentXmlValidator> logger)
    {
        _logger = logger;
        _facturaSchemas = new Lazy<XmlSchemaSet>(LoadFacturaSchemas);
        _notaCreditoSchemas = new Lazy<XmlSchemaSet>(LoadNotaCreditoSchemas);
    }

    public XmlValidationResult Validate(byte[] xml, ElectronicDocumentSchema schema)
    {
        ArgumentNullException.ThrowIfNull(xml);

        var schemas = schema switch
        {
            ElectronicDocumentSchema.FacturaV110 => _facturaSchemas.Value,
            ElectronicDocumentSchema.NotaCreditoV110 => _notaCreditoSchemas.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(schema), schema, "Esquema no soportado."),
        };

        var errors = new List<string>();
        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemas,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };
        settings.ValidationEventHandler += (_, e) =>
        {
            errors.Add($"{e.Severity}: {e.Message}");
        };

        try
        {
            using var stream = new MemoryStream(xml, writable: false);
            using var reader = XmlReader.Create(stream, settings);
            while (reader.Read())
            {
                // Fuerza validación completa.
            }
        }
        catch (XmlException ex)
        {
            errors.Add($"XML mal formado: {ex.Message}");
        }
        catch (XmlSchemaException ex)
        {
            errors.Add($"XSD: {ex.Message}");
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning(
                "Validación XSD {Schema} falló con {Count} error(es)",
                schema,
                errors.Count);
            return XmlValidationResult.Fail(errors);
        }

        return XmlValidationResult.Ok();
    }

    private static XmlSchemaSet LoadFacturaSchemas()
    {
        var xsdPath = ResolveSchemaPath(
            Path.Combine("xml", "Factura", "Factura_V1.1.0.xsd"));

        var set = new XmlSchemaSet();
        using var reader = XmlReader.Create(xsdPath);
        set.Add(null, reader);
        set.Compile();
        return set;
    }

    private static XmlSchemaSet LoadNotaCreditoSchemas()
    {
        var xsdPath = ResolveSchemaPath(
            Path.Combine("xml", "notaDeCredito", "ValidadorNotaCredito_V1.1.0.xsd"));
        var dir = Path.GetDirectoryName(xsdPath) ?? ".";
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = new XmlUrlResolver(),
        };

        var set = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
        var dsigPath = Path.Combine(dir, "xmldsig-core-schema.xsd");
        if (File.Exists(dsigPath))
        {
            using var dsigReader = XmlReader.Create(dsigPath, settings);
            set.Add("http://www.w3.org/2000/09/xmldsig#", dsigReader);
        }

        using var reader = XmlReader.Create(xsdPath, settings);
        set.Add(null, reader);
        set.Compile();
        return set;
    }

    private static string ResolveSchemaPath(string relativePath)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, relativePath),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", relativePath)),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath)),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", relativePath)),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException(
            $"No se encontró el XSD '{relativePath}'. Copie xml/Factura y xml/notaDeCredito al output de la API.",
            relativePath);
    }
}
