using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents;
using Ecunexo.Billing.Domain.Emitter;
using Ecunexo.Billing.Domain.Emitter.Ports;
using Ecunexo.Billing.Domain.TaxCatalog;
using Ecunexo.Billing.Domain.TaxCatalog.Ports;
using Ecunexo.Billing.Infrastructure.Xml;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ecunexo.Billing.Infrastructure.Tests.Xml;

public class SriFacturaXmlGeneratorTests
{
    [Fact(DisplayName = "XML factura v1.1.0 pasa validación XSD")]
    public void BuildXml_PassesXsdValidation()
    {
        var line = new InvoiceLine(
            1,
            "Servicio demo",
            2m,
            new Money(50m),
            Money.Zero,
            new Money(100m),
            [new LineTax("2", "4", 15m, new Money(100m), new Money(15m))]);

        var invoice = ElectronicInvoice.Create(
            Ruc.Create("1792146739001"),
            EstablishmentCode.Create("001"),
            EmissionPoint.Create("001"),
            SequentialNumber.Create("000000001"),
            new DateOnly(2024, 1, 21),
            Counterparty.Create("04", "0999999999001", "Cliente Demo SA", "Quito"),
            [line],
            BuildTaxRepo());

        var accessKey = ClaveAcceso.Create(new ClaveAccesoComponents(
            invoice.IssueDate,
            invoice.DocumentType,
            invoice.EmitterRuc,
            "1",
            invoice.Establishment,
            invoice.EmissionPoint,
            invoice.Sequential,
            12345678,
            "1"));
        invoice.MarkSigned(accessKey);

        var generator = new SriFacturaXmlGenerator();
        var emitter = new InvoiceXmlEmitterContext(
            "Emisor Demo Cia Ltda",
            "Av. Principal 123",
            "Emisor Demo");

        var xml = generator.BuildXml(invoice, emitter, accessKey);
        var xmlText = System.Text.Encoding.UTF8.GetString(xml);

        Assert.Contains("<factura id=\"comprobante\" version=\"1.1.0\"", xmlText);
        Assert.Contains("<tipoEmision>1</tipoEmision>", xmlText);
        Assert.Contains("<codDoc>01</codDoc>", xmlText);
        Assert.Contains("<importeTotal>115.00</importeTotal>", xmlText);

        var validator = new XsdElectronicDocumentXmlValidator(NullLogger<XsdElectronicDocumentXmlValidator>.Instance);
        var result = validator.Validate(xml, ElectronicDocumentSchema.FacturaV110);

        Assert.True(result.IsValid, string.Join(" | ", result.Errors));
    }

    private static ITaxRateRepository BuildTaxRepo()
    {
        var repository = new InMemoryTaxRepo();
        repository.Add(TaxRate.Create("2", "4", "IVA 15%", 15m, new DateOnly(2020, 1, 1)));
        return repository;
    }

    private sealed class InMemoryTaxRepo : ITaxRateRepository
    {
        private readonly List<TaxRate> _rates = [];

        public void Add(TaxRate rate) => _rates.Add(rate);

        public TaxRate? GetByRateCodes(string taxCode, string rateCode, DateOnly date) =>
            _rates.FirstOrDefault(r => r.TaxCode == taxCode && r.RateCode == rateCode && r.IsActiveOn(date));

        public IReadOnlyList<TaxRate> GetActiveByTaxCode(string taxCode, DateOnly date) =>
            _rates.Where(r => r.TaxCode == taxCode && r.IsActiveOn(date)).ToList();
    }

    [Fact(DisplayName = "XML usa código principal e información adicional")]
    public void BuildXml_WritesMainCodeAndAdditionalNote()
    {
        var line = new InvoiceLine(
            1,
            "PROMOCION CALCETINES DOCENA",
            1m,
            new Money(29.56m),
            Money.Zero,
            new Money(29.56m),
            [new LineTax("2", "4", 15m, new Money(29.56m), new Money(4.43m))],
            "0100");

        var invoice = ElectronicInvoice.Create(
            Ruc.Create("1792146739001"),
            EstablishmentCode.Create("001"),
            EmissionPoint.Create("001"),
            SequentialNumber.Create("000000001"),
            new DateOnly(2024, 1, 21),
            Counterparty.Create("04", "0999999999001", "Cliente Demo SA", "Quito", "a@b.com", "0990000000"),
            [line],
            BuildTaxRepo(),
            "20",
            "PROMOCION AGOSTO 2026",
            0);

        var accessKey = ClaveAcceso.Create(new ClaveAccesoComponents(
            invoice.IssueDate,
            invoice.DocumentType,
            invoice.EmitterRuc,
            "1",
            invoice.Establishment,
            invoice.EmissionPoint,
            invoice.Sequential,
            12345678,
            "1"));
        invoice.MarkSigned(accessKey);

        var xml = new SriFacturaXmlGenerator().BuildXml(
            invoice,
            new InvoiceXmlEmitterContext("Emisor Demo Cia Ltda", "Av. Principal 123", "Emisor Demo"),
            accessKey);
        var xmlText = System.Text.Encoding.UTF8.GetString(xml);

        Assert.Contains("<codigoPrincipal>0100</codigoPrincipal>", xmlText);
        Assert.Contains("nombre=\"Descripcion\">PROMOCION AGOSTO 2026</campoAdicional>", xmlText);
    }

    [Fact(DisplayName = "XML incluye RUC Proveedor en infoAdicional")]
    public void BuildXml_WritesRucProveedor()
    {
        var line = new InvoiceLine(
            1,
            "Servicio demo",
            1m,
            new Money(100m),
            Money.Zero,
            new Money(100m),
            [new LineTax("2", "4", 15m, new Money(100m), new Money(15m))]);

        var invoice = ElectronicInvoice.Create(
            Ruc.Create("1792146739001"),
            EstablishmentCode.Create("001"),
            EmissionPoint.Create("001"),
            SequentialNumber.Create("000000001"),
            new DateOnly(2024, 1, 21),
            Counterparty.Create("04", "0999999999001", "Cliente Demo SA", "Quito"),
            [line],
            BuildTaxRepo());

        var accessKey = ClaveAcceso.Create(new ClaveAccesoComponents(
            invoice.IssueDate,
            invoice.DocumentType,
            invoice.EmitterRuc,
            "1",
            invoice.Establishment,
            invoice.EmissionPoint,
            invoice.Sequential,
            12345678,
            "1"));
        invoice.MarkSigned(accessKey);

        var xml = new SriFacturaXmlGenerator().BuildXml(
            invoice,
            new InvoiceXmlEmitterContext(
                "Emisor Demo Cia Ltda",
                "Av. Principal 123",
                "Emisor Demo",
                SoftwareProviderRuc: "0993397804001"),
            accessKey);
        var xmlText = System.Text.Encoding.UTF8.GetString(xml);

        Assert.Contains("nombre=\"RUC Proveedor\">0993397804001</campoAdicional>", xmlText);

        var validator = new XsdElectronicDocumentXmlValidator(NullLogger<XsdElectronicDocumentXmlValidator>.Instance);
        var result = validator.Validate(xml, ElectronicDocumentSchema.FacturaV110);
        Assert.True(result.IsValid, string.Join(" | ", result.Errors));
    }
}
