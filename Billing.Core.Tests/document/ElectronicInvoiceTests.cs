using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents;
using Ecunexo.Billing.Core.Tests.TaxCatalog;
using Ecunexo.Billing.Core.TaxCatalog.Ports;


namespace Ecunexo.Billing.Core.Tests.Documents;

public class ElectronicInvoiceTests
{
    [Fact(DisplayName = "Factura se crea con tipo 01 y al menos una línea")]
    public void Create_WithOneLine_ShouldSucceed()
    {
        var invoice = BuildInvoice();

        Assert.Equal("01", invoice.DocumentType.Value);
        Assert.Single(invoice.Lines);
        Assert.Equal(SriDocumentState.Draft, invoice.State);
        Assert.Equal("Cliente Prueba", invoice.Counterparty.BusinessName);
        Assert.Equal(100m, invoice.SubtotalWithoutTax.Amount);
        Assert.Single(invoice.TaxTotals);
        Assert.Equal(15m, invoice.TaxTotals[0].Value.Amount);
        Assert.Equal(115m, invoice.GrandTotal.Amount);
    }

    [Fact(DisplayName = "Factura sin líneas es rechazada")]
    public void Create_WithNoLines_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ElectronicInvoice.Create(
                Ruc.Create("1792146739001"),
                EstablishmentCode.Create("001"),
                EmissionPoint.Create("001"),
                SequentialNumber.Create("000000001"),
                new DateOnly(2024, 1, 21),
                Counterparty.Create("04", "0999999999001", "Cliente"),
                [],
                TestTaxRateCatalog.WithIva15()));
    }

    private static ElectronicInvoice BuildInvoice()
    {
        var line = new InvoiceLine(
            1,
            "Servicio",
            2m,
            new Money(50m),
            Money.Zero,
            new Money(100m),
            [new LineTax("2", "4", 15m, new Money(100m), new Money(15m))]);

        return ElectronicInvoice.Create(
            Ruc.Create("1792146739001"),
            EstablishmentCode.Create("001"),
            EmissionPoint.Create("001"),
            SequentialNumber.Create("000000001"),
            new DateOnly(2024, 1, 21),
            Counterparty.Create("04", "0999999999001", "Cliente Prueba"),
            [line],
            TestTaxRateCatalog.WithIva15());
    }

    [Fact(DisplayName = "Create calcula totales desde las líneas")]
    public void Create_WithOneLine_CalculatesTotals()
    {
        var invoice = BuildInvoice();

        Assert.Equal(100m, invoice.SubtotalWithoutTax.Amount);
        Assert.Equal(115m, invoice.GrandTotal.Amount);
    }

    [Fact(DisplayName = "Factura con línea inconsistente es rechazada")]
    public void Create_WithInvalidLineTotal_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ElectronicInvoice.Create(
                Ruc.Create("1792146739001"),
                EstablishmentCode.Create("001"),
                EmissionPoint.Create("001"),
                SequentialNumber.Create("000000001"),
                new DateOnly(2024, 1, 21),
                Counterparty.Create("04", "0999999999001", "Cliente"),
                [
                    new InvoiceLine(
                    1,
                    "Servicio",
                    2m,
                    new Money(50m),
                    Money.Zero,
                    new Money(99m),
                    [new LineTax("2", "4", 15m, new Money(99m), new Money(14.85m))])
                ],
                TestTaxRateCatalog.WithIva15()));
    }

    [Fact(DisplayName = "Create rechaza tarifa no vigente en catálogo")]
    public void Create_WithRateNotInCatalog_Throws()
    {
        var line = new InvoiceLine(
            1, "Servicio", 1m, new Money(100m), Money.Zero, new Money(100m),
            [new LineTax("2", "4", 12m, new Money(100m), new Money(12m))]);
        Assert.Throws<ArgumentException>(() =>
            ElectronicInvoice.Create(
                Ruc.Create("1792146739001"),
                EstablishmentCode.Create("001"),
                EmissionPoint.Create("001"),
                SequentialNumber.Create("000000001"),
                new DateOnly(2024, 1, 21),
                Counterparty.Create("04", "0999999999001", "Cliente"),
                [line],
                TestTaxRateCatalog.WithIva15()));
    }

    [Fact(DisplayName = "Create rechaza fecha de emisión vacía")]
    public void Create_WithDefaultIssueDate_Throws()
    {
        var line = new InvoiceLine(
            1, "Servicio", 1m, new Money(10m), Money.Zero, new Money(10m),
            [new LineTax("2", "4", 15m, new Money(10m), new Money(1.5m))]);
        Assert.Throws<ArgumentException>(() =>
            ElectronicInvoice.Create(
                Ruc.Create("1792146739001"),
                EstablishmentCode.Create("001"),
                EmissionPoint.Create("001"),
                SequentialNumber.Create("000000001"),
                default,
                Counterparty.Create("04", "0999999999001", "Cliente"),
                [line],
                TestTaxRateCatalog.WithIva15()));
    }

    [Fact(DisplayName = "Create rechaza Consumidor Final con total mayor a USD 50")]
    public void Create_ConsumidorFinal_Exceeding50_Throws()
    {
        var line = new InvoiceLine(
            1, "Producto", 1m, new Money(50m), Money.Zero, new Money(50m),
            [new LineTax("2", "4", 15m, new Money(50m), new Money(7.5m))]); // Total = 57.50 > 50

        var ex = Assert.Throws<ArgumentException>(() =>
            ElectronicInvoice.Create(
                Ruc.Create("1792146739001"),
                EstablishmentCode.Create("001"),
                EmissionPoint.Create("001"),
                SequentialNumber.Create("000000001"),
                new DateOnly(2024, 1, 21),
                Counterparty.Create("07", "9999999999999", "CONSUMIDOR FINAL"),
                [line],
                TestTaxRateCatalog.WithIva15()));

        Assert.Contains("50.00", ex.Message);
    }

    [Fact(DisplayName = "Create acepta Consumidor Final con total menor o igual a USD 50")]
    public void Create_ConsumidorFinal_Under50_Succeeds()
    {
        var line = new InvoiceLine(
            1, "Producto", 1m, new Money(30m), Money.Zero, new Money(30m),
            [new LineTax("2", "4", 15m, new Money(30m), new Money(4.5m))]); // Total = 34.50 <= 50

        var invoice = ElectronicInvoice.Create(
            Ruc.Create("1792146739001"),
            EstablishmentCode.Create("001"),
            EmissionPoint.Create("001"),
            SequentialNumber.Create("000000001"),
            new DateOnly(2024, 1, 21),
            Counterparty.Create("07", "9999999999999", "CONSUMIDOR FINAL"),
            [line],
            TestTaxRateCatalog.WithIva15());

        Assert.Equal(34.50m, invoice.GrandTotal.Amount);
    }

    [Fact(DisplayName = "Counterparty rechaza RUC que no tiene 13 dígitos numéricos")]
    public void Counterparty_InvalidRuc_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Counterparty.Create("04", "123456", "Empresa Test"));
        Assert.Throws<ArgumentException>(() =>
            Counterparty.Create("04", "179214673900A", "Empresa Test"));
    }

    [Fact(DisplayName = "Counterparty rechaza Cédula que no tiene 10 dígitos numéricos")]
    public void Counterparty_InvalidCedula_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Counterparty.Create("05", "12345", "Persona Test"));
        Assert.Throws<ArgumentException>(() =>
            Counterparty.Create("05", "171234567A", "Persona Test"));
    }

    [Fact(DisplayName = "Counterparty rechaza tipo de identificación no SRI")]
    public void Counterparty_InvalidTipoId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Counterparty.Create("99", "1712345678", "Persona Test"));
    }
}

