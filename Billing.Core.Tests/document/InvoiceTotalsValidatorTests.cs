using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents;
using Ecunexo.Billing.Core.Documents.Services;
using Ecunexo.Billing.Core.Tests.TaxCatalog;

namespace Ecunexo.Billing.Core.Tests.Documents;

public class InvoiceTotalsValidatorTests
{
    [Fact(DisplayName = "Totales coherentes con las líneas no lanzan")]
    public void Validate_CoherentTotals_DoesNotThrow()
    {
        var lines = new[] { Line(100m, 15m) };

        var (subtotal, taxTotals, grandTotal) = InvoiceTotalsCalculator.Calculate(lines);

        InvoiceTotalsValidator.Validate(lines, subtotal, taxTotals, grandTotal);
    }

    [Fact(DisplayName = "importeTotal distinto a subtotal + impuestos falla")]
    public void Validate_WrongGrandTotal_Throws()
    {
        var lines = new[] { Line(100m, 15m) };
        var (subtotal, taxTotals, _) = InvoiceTotalsCalculator.Calculate(lines);

        var ex = Assert.Throws<ArgumentException>(() =>
            InvoiceTotalsValidator.Validate(lines, subtotal, taxTotals, new Money(114m)));

        Assert.Contains("importeTotal", ex.Message);
    }

    [Fact(DisplayName = "totalSinImpuestos distinto a suma de líneas falla")]
    public void Validate_WrongSubtotal_Throws()
    {
        var lines = new[] { Line(100m, 15m) };
        var (_, taxTotals, grandTotal) = InvoiceTotalsCalculator.Calculate(lines);

        var ex = Assert.Throws<ArgumentException>(() =>
            InvoiceTotalsValidator.Validate(lines, new Money(99m), taxTotals, grandTotal));

        Assert.Contains("totalSinImpuestos", ex.Message);
    }

    [Fact(DisplayName = "valor en cabecera distinto al agrupado desde líneas falla")]
    public void Validate_WrongHeaderTaxValue_Throws()
    {
        var lines = new[] { Line(100m, 15m) };
        var (subtotal, _, grandTotal) = InvoiceTotalsCalculator.Calculate(lines);

        var wrongTaxTotals = new[]
        {
            new DocumentTaxTotal("2", "4", new Money(100m), new Money(14m))
        };

        var ex = Assert.Throws<ArgumentException>(() =>
            InvoiceTotalsValidator.Validate(lines, subtotal, wrongTaxTotals, grandTotal));

        Assert.Contains("valor en cabecera", ex.Message);
    }

    [Fact(DisplayName = "Factura creada pasa validación de cabecera")]
    public void Validate_AfterCreate_DoesNotThrow()
    {
        var invoice = ElectronicInvoice.Create(
            Ruc.Create("1792146739001"),
            EstablishmentCode.Create("001"),
            EmissionPoint.Create("001"),
            SequentialNumber.Create("000000001"),
            new DateOnly(2024, 1, 21),
            Counterparty.Create("04", "0999999999001", "Cliente"),
            [Line(100m, 15m)],
            TestTaxRateCatalog.WithIva15());

        InvoiceTotalsValidator.Validate(invoice);
    }

    [Fact(DisplayName = "Dos líneas mismo impuesto: cabecera agrupada válida")]
    public void Validate_TwoLinesAggregatedTax_DoesNotThrow()
    {
        var lines = new[]
        {
            Line(50m, 7.5m),
            Line(50m, 7.5m)
        };

        var (subtotal, taxTotals, grandTotal) = InvoiceTotalsCalculator.Calculate(lines);

        Assert.Equal(100m, subtotal.Amount);
        Assert.Single(taxTotals);
        Assert.Equal(15m, taxTotals[0].Value.Amount);
        Assert.Equal(115m, grandTotal.Amount);

        InvoiceTotalsValidator.Validate(lines, subtotal, taxTotals, grandTotal);
    }

    [Fact(DisplayName = "Impuestos duplicados en cabecera son rechazados")]
    public void Validate_DuplicateHeaderTaxes_Throws()
    {
        var lines = new[]
        {
            new InvoiceLine(
                1,
                "Item A",
                1m,
                new Money(100m),
                Money.Zero,
                new Money(100m),
                [new LineTax("2", "4", 15m, new Money(100m), new Money(15m))]),
            new InvoiceLine(
                2,
                "Item B",
                1m,
                new Money(50m),
                Money.Zero,
                new Money(50m),
                [new LineTax("2", "0", 0m, new Money(50m), new Money(0m))])
        };

        var (subtotal, _, grandTotal) = InvoiceTotalsCalculator.Calculate(lines);

        var duplicateTaxTotals = new[]
        {
            new DocumentTaxTotal("2", "4", new Money(100m), new Money(15m)),
            new DocumentTaxTotal("2", "4", new Money(100m), new Money(15m))
        };

        var ex = Assert.Throws<ArgumentException>(() =>
            InvoiceTotalsValidator.Validate(lines, subtotal, duplicateTaxTotals, grandTotal));

        Assert.Contains("duplicados", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static InvoiceLine Line(decimal lineTotal, decimal taxValue)
    {
        return new InvoiceLine(
            1,
            "Item",
            1m,
            new Money(lineTotal),
            Money.Zero,
            new Money(lineTotal),
            [new LineTax("2", "4", 15m, new Money(lineTotal), new Money(taxValue))]);
    }
}
