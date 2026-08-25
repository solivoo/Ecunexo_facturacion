using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents;
using Ecunexo.Billing.Domain.Emitter;
using Ecunexo.Billing.Domain.Emitter.Ports;
using Ecunexo.Billing.Domain.TaxCatalog;
using Ecunexo.Billing.Domain.TaxCatalog.Ports;
using Ecunexo.Billing.Infrastructure.Xml;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ecunexo.Billing.Infrastructure.Tests.Xml;

public class SriNotaCreditoXmlGeneratorTests
{
    [Fact(DisplayName = "XML nota de crédito v1.1.0 pasa validación XSD")]
    public void BuildXml_PassesXsdValidation()
    {
        var note = BuildAuthorizedCreditNote();
        var accessKey = ClaveAcceso.Create(new ClaveAccesoComponents(
            note.IssueDate,
            note.DocumentType,
            note.EmitterRuc,
            "1",
            note.Establishment,
            note.EmissionPoint,
            note.Sequential,
            12345678,
            "1"));
        note.MarkSigned(accessKey);

        var xml = new SriNotaCreditoXmlGenerator().BuildXml(
            note,
            new InvoiceXmlEmitterContext("Emisor Demo Cia Ltda", "Av. Principal 123", "Emisor Demo"),
            accessKey);
        var xmlText = System.Text.Encoding.UTF8.GetString(xml);

        Assert.Contains("<notaCredito id=\"comprobante\" version=\"1.1.0\"", xmlText);
        Assert.Contains("<codDoc>04</codDoc>", xmlText);
        Assert.Contains("<codDocModificado>01</codDocModificado>", xmlText);
        Assert.Contains("<numDocModificado>001-001-000000001</numDocModificado>", xmlText);
        Assert.Contains("<valorModificacion>115.00</valorModificacion>", xmlText);
        Assert.Contains("<motivo>Anulación total</motivo>", xmlText);
        Assert.Contains("<codigoInterno>", xmlText);

        var validator = new XsdElectronicDocumentXmlValidator(NullLogger<XsdElectronicDocumentXmlValidator>.Instance);
        var result = validator.Validate(xml, ElectronicDocumentSchema.NotaCreditoV110);

        Assert.True(result.IsValid, string.Join(" | ", result.Errors));
    }

    private static ElectronicCreditNote BuildAuthorizedCreditNote()
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

        var invoiceKey = ClaveAcceso.Create(new ClaveAccesoComponents(
            invoice.IssueDate,
            invoice.DocumentType,
            invoice.EmitterRuc,
            "1",
            invoice.Establishment,
            invoice.EmissionPoint,
            invoice.Sequential,
            11111111,
            "1"));
        invoice.MarkSigned(invoiceKey);
        invoice.MarkReceived();
        invoice.MarkProcessing();
        invoice.MarkAuthorized();

        return ElectronicCreditNote.CreateFromAuthorizedInvoice(
            invoice,
            SequentialNumber.Create("000000001"),
            new DateOnly(2024, 2, 1),
            "Anulación total");
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
}
