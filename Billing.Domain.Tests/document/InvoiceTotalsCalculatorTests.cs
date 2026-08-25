using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents;
using Ecunexo.Billing.Domain.Documents.Services;

namespace Ecunexo.Billing.Domain.Tests.Documents;

public class InvoiceTotalsCalculatorTests
{
    [Fact(DisplayName = "Una línea: subtotal 100, IVA 15, total 115")]
    public void Calculate_OneLineWithIva_ReturnsExpectedTotals()
    {
        var lines = new[] { Line(100m, "4", 15m, 15m) };

        var (subtotal, taxTotals, grandTotal) = InvoiceTotalsCalculator.Calculate(lines);

        Assert.Equal(100m, subtotal.Amount);
        Assert.Single(taxTotals);
        Assert.Equal("2", taxTotals[0].TaxCode);
        Assert.Equal("4", taxTotals[0].RateCode);
        Assert.Equal(100m, taxTotals[0].TaxableBase.Amount);
        Assert.Equal(15m, taxTotals[0].Value.Amount);
        Assert.Equal(115m, grandTotal.Amount);
    }

    [Fact(DisplayName = "Dos líneas mismo impuesto: agrupa en un DocumentTaxTotal")]
    public void Calculate_TwoLinesSameTaxCode_AggregatesTaxTotals()
    {
        var lines = new[]
        {
            Line(50m, "4", 15m, 7.5m),
            Line(50m, "4", 15m, 7.5m)
        };

        var (subtotal, taxTotals, grandTotal) = InvoiceTotalsCalculator.Calculate(lines);

        Assert.Equal(100m, subtotal.Amount);
        Assert.Single(taxTotals);
        Assert.Equal(100m, taxTotals[0].TaxableBase.Amount);
        Assert.Equal(15m, taxTotals[0].Value.Amount);
        Assert.Equal(115m, grandTotal.Amount);
    }

    [Fact(DisplayName = "Dos líneas distinto RateCode: dos DocumentTaxTotal")]
    public void Calculate_TwoLinesDifferentRateCode_ReturnsTwoTaxTotals()
    {
        var lines = new[]
        {
            Line(100m, "4", 15m, 15m),
            Line(50m, "0", 0m, 0m)
        };

        var (_, taxTotals, grandTotal) = InvoiceTotalsCalculator.Calculate(lines);

        Assert.Equal(2, taxTotals.Count);
        Assert.Equal(165m, grandTotal.Amount);
    }

    [Fact(DisplayName = "Sin líneas lanza ArgumentException")]
    public void Calculate_NoLines_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            InvoiceTotalsCalculator.Calculate([]));
    }

    private static InvoiceLine Line(
        decimal lineTotal,
        string rateCode,
        decimal rate,
        decimal taxValue)
    {
        return new InvoiceLine(
            1,
            "Item",
            1m,
            new Money(lineTotal),
            Money.Zero,
            new Money(lineTotal),
            [new LineTax("2", rateCode, rate, new Money(lineTotal), new Money(taxValue))]);
    }
}