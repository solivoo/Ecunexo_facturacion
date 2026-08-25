using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents;
using Ecunexo.Billing.Domain.Emitter;
using Ecunexo.Billing.Domain.Emitter.Ports;
using Ecunexo.Billing.Infrastructure.Ride;

namespace Ecunexo.Billing.Infrastructure.Xml;

/// <summary>Genera XML Factura SRI v1.1.0 (sin firma) listo para validar contra XSD.</summary>
public sealed class SriFacturaXmlGenerator : IElectronicInvoiceXmlGenerator
{
    public byte[] BuildXml(
        ElectronicInvoice invoice,
        InvoiceXmlEmitterContext emitter,
        ClaveAcceso accessKey)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(emitter);
        ArgumentNullException.ThrowIfNull(accessKey);

        if (string.IsNullOrWhiteSpace(emitter.BusinessName))
            throw new ArgumentException("La razón social del emisor es obligatoria.", nameof(emitter));
        if (string.IsNullOrWhiteSpace(emitter.MainAddress))
            throw new ArgumentException("La dirección matriz del emisor es obligatoria.", nameof(emitter));

        var totalDiscount = invoice.Lines.Sum(l => l.Discount.Amount);

        var infoTributaria = new XElement(
            "infoTributaria",
            new XElement("ambiente", emitter.EnvironmentCode),
            new XElement("tipoEmision", emitter.EmissionTypeCode),
            new XElement("razonSocial", Sanitize(emitter.BusinessName)),
            string.IsNullOrWhiteSpace(emitter.TradeName)
                ? null
                : new XElement("nombreComercial", Sanitize(emitter.TradeName)),
            new XElement("ruc", invoice.EmitterRuc.Value),
            new XElement("claveAcceso", accessKey.Value),
            new XElement("codDoc", invoice.DocumentType.Value),
            new XElement("estab", invoice.Establishment.Value),
            new XElement("ptoEmi", invoice.EmissionPoint.Value),
            new XElement("secuencial", invoice.Sequential.Value),
            new XElement("dirMatriz", Sanitize(emitter.MainAddress)));

        var totalConImpuestos = new XElement(
            "totalConImpuestos",
            invoice.TaxTotals.Select(t => new XElement(
                "totalImpuesto",
                new XElement("codigo", t.TaxCode),
                new XElement("codigoPorcentaje", t.RateCode),
                new XElement("baseImponible", Money2(t.TaxableBase.Amount)),
                new XElement("valor", Money2(t.Value.Amount)))));

        var pagos = new XElement(
            "pagos",
            new XElement(
                "pago",
                new XElement("formaPago", invoice.PaymentFormCode),
                new XElement("total", Money2(invoice.GrandTotal.Amount))));

        var infoFactura = new XElement(
            "infoFactura",
            new XElement("fechaEmision", FormatDate(invoice.IssueDate)),
            new XElement("obligadoContabilidad", "NO"),
            new XElement("tipoIdentificacionComprador", invoice.Counterparty.IdentificationType),
            new XElement("razonSocialComprador", Sanitize(invoice.Counterparty.BusinessName)),
            new XElement("identificacionComprador", Sanitize(invoice.Counterparty.Identification)),
            string.IsNullOrWhiteSpace(invoice.Counterparty.Address)
                ? null
                : new XElement("direccionComprador", Sanitize(invoice.Counterparty.Address)),
            new XElement("totalSinImpuestos", Money2(invoice.SubtotalWithoutTax.Amount)),
            new XElement("totalDescuento", Money2(totalDiscount)),
            totalConImpuestos,
            new XElement("propina", Money2(0m)),
            new XElement("importeTotal", Money2(invoice.GrandTotal.Amount)),
            new XElement("moneda", "DOLAR"),
            pagos);

        var detalles = new XElement(
            "detalles",
            invoice.Lines.OrderBy(l => l.LineNumber).Select(BuildDetalle));

        var factura = new XElement(
            "factura",
            new XAttribute("id", "comprobante"),
            new XAttribute("version", "1.1.0"),
            infoTributaria,
            infoFactura,
            detalles,
            BuildInfoAdicional(invoice, emitter.SoftwareProviderRuc));

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), factura);
        using var ms = new MemoryStream();
        using (var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
        {
            doc.Save(writer, SaveOptions.DisableFormatting);
        }

        return ms.ToArray();
    }

    private static XElement BuildDetalle(InvoiceLine line)
    {
        if (line.Taxes.Count == 0)
            throw new InvalidOperationException($"La línea {line.LineNumber} no tiene impuestos.");

        var impuestos = new XElement(
            "impuestos",
            line.Taxes.Select(t => new XElement(
                "impuesto",
                new XElement("codigo", t.TaxCode),
                new XElement("codigoPorcentaje", t.RateCode),
                new XElement("tarifa", Money2(t.Rate)),
                new XElement("baseImponible", Money2(t.TaxableBase.Amount)),
                new XElement("valor", Money2(t.Value.Amount)))));

        return new XElement(
            "detalle",
            new XElement("codigoPrincipal", BuildProductCode(line)),
            new XElement("descripcion", Sanitize(line.Description)),
            new XElement("cantidad", Qty(line.Quantity)),
            new XElement("precioUnitario", Qty(line.UnitPrice.Amount)),
            new XElement("descuento", Money2(line.Discount.Amount)),
            new XElement("precioTotalSinImpuesto", Money2(line.LineTotalWithoutTax.Amount)),
            impuestos);
    }

    private static XElement? BuildInfoAdicional(ElectronicInvoice invoice, string? softwareProviderRuc)
    {
        var campos = new List<XElement>();
        AddCampo(campos, RideProviderOptions.CampoNombreRucProveedor, softwareProviderRuc);

        if (!string.IsNullOrWhiteSpace(invoice.AdditionalNote))
            AddCampo(campos, "Descripcion", invoice.AdditionalNote);

        var counterparty = invoice.Counterparty;
        if (!string.IsNullOrWhiteSpace(counterparty.Email))
            AddCampo(campos, "Email", counterparty.Email);

        if (!string.IsNullOrWhiteSpace(counterparty.Phone))
            AddCampo(campos, "Telefono", counterparty.Phone);

        return campos.Count == 0 ? null : new XElement("infoAdicional", campos);
    }

    private static void AddCampo(List<XElement> campos, string nombre, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        campos.Add(new XElement(
            "campoAdicional",
            new XAttribute("nombre", nombre),
            RideProviderOptions.TruncateCampo(Sanitize(value))));
    }

    private static string BuildProductCode(InvoiceLine line)
    {
        if (!string.IsNullOrWhiteSpace(line.MainCode))
        {
            var code = Sanitize(line.MainCode);
            return code.Length <= 25 ? code : code[..25];
        }

        var raw = new string(line.Description
            .Where(char.IsLetterOrDigit)
            .Take(20)
            .ToArray());

        if (string.IsNullOrWhiteSpace(raw))
            return $"ITEM{line.LineNumber:D3}";

        return raw.Length <= 25 ? raw : raw[..25];
    }

    private static string FormatDate(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string Money2(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Qty(decimal value) =>
        value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string Sanitize(string value) =>
        value.Replace('\n', ' ').Replace('\r', ' ').Trim();
}
