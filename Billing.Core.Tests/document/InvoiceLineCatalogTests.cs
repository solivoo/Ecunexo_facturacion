using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents;
using Ecunexo.Billing.Core.Documents.Services;
using Ecunexo.Billing.Core.TaxCatalog;
using Ecunexo.Billing.Core.Tests.TaxCatalog;

namespace Ecunexo.Billing.Core.Tests.Documents;

public class InvoiceLineCatalogTests
{
    private static readonly DateOnly IssueDate = new(2024, 6, 1);

    [Fact(DisplayName = "Tarifa línea coincide con catálogo vigente")]
    public void ValidateAgainstCatalog_MatchingRate_DoesNotThrow()
    {
        var repo = TestTaxRateCatalog.WithIva15();
        var lines = new[] { LineWithTax(rate: 15m, taxValue: 15m) };

        InvoiceLineValidator.ValidateAgainstCatalog(lines, IssueDate, repo);
    }

    [Fact(DisplayName = "Tarifa distinta al catálogo falla")]
    public void ValidateAgainstCatalog_WrongRate_Throws()
    {
        var repo = TestTaxRateCatalog.WithIva15();
        var lines = new[] { LineWithTax(rate: 12m, taxValue: 12m) };

        Action act = () => InvoiceLineValidator.ValidateAgainstCatalog(lines, IssueDate, repo);
        var ex = Assert.Throws<ArgumentException>(act);

        Assert.Contains("catálogo vigente", ex.Message);
    }

    [Fact(DisplayName = "codigoPorcentaje sin tarifa vigente falla")]
    public void ValidateAgainstCatalog_UnknownRateCode_Throws()
    {
        var repo = TestTaxRateCatalog.WithIva15();
        var lines = new[]
        {
            new InvoiceLine(
                1, "Item", 1m, new Money(100m), Money.Zero, new Money(100m),
                [new LineTax("2", "99", 15m, new Money(100m), new Money(15m))])
        };

        Action act = () => InvoiceLineValidator.ValidateAgainstCatalog(lines, IssueDate, repo);
        var ex = Assert.Throws<ArgumentException>(act);

        Assert.Contains("catálogo", ex.Message);
    }

    [Fact(DisplayName = "Línea sin impuestos no consulta catálogo")]
    public void ValidateAgainstCatalog_NoTaxes_DoesNotThrow()
    {
        var repo = TestTaxRateCatalog.WithIva15();
        var lines = new[]
        {
            new InvoiceLine(1, "Item", 1m, new Money(10m), Money.Zero, new Money(10m), [])
        };

        InvoiceLineValidator.ValidateAgainstCatalog(lines, IssueDate, repo);
    }

    [Fact(DisplayName = "Tarifa fuera de vigencia en fecha de emisión falla")]
    public void ValidateAgainstCatalog_ExpiredRate_Throws()
    {
        var repo = new InMemoryTaxRateRepository();
        repo.Add(TaxRate.Create(
            "2",
            "4",
            "IVA 15% temporal",
            15m,
            new DateOnly(2023, 1, 1),
            new DateOnly(2023, 12, 31)));

        var lines = new[] { LineWithTax(rate: 15m, taxValue: 15m) };

        Action act = () => InvoiceLineValidator.ValidateAgainstCatalog(lines, IssueDate, repo);
        var ex = Assert.Throws<ArgumentException>(act);

        Assert.Contains("No existe tarifa vigente", ex.Message);
    }

    [Fact(DisplayName = "Tarifa vigente exacta por fecha límite es válida")]
    public void ValidateAgainstCatalog_RateValidOnBoundary_DoesNotThrow()
    {
        var repo = new InMemoryTaxRateRepository();
        var boundaryDate = new DateOnly(2024, 6, 1);
        repo.Add(TaxRate.Create(
            "2",
            "4",
            "IVA 15%",
            15m,
            new DateOnly(2024, 1, 1),
            boundaryDate));

        var lines = new[] { LineWithTax(rate: 15m, taxValue: 15m) };

        InvoiceLineValidator.ValidateAgainstCatalog(lines, boundaryDate, repo);
    }

    private static InvoiceLine LineWithTax(decimal rate, decimal taxValue)
    {
        return new InvoiceLine(
            1,
            "Item",
            1m,
            new Money(100m),
            Money.Zero,
            new Money(100m),
            [new LineTax("2", "4", rate, new Money(100m), new Money(taxValue))]);
    }
}