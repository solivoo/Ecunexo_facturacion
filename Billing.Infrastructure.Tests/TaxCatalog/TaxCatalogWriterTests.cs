using Ecunexo.Billing.Core.TaxCatalog.Ports;
using Ecunexo.Billing.Infrastructure.Persistence;
using Ecunexo.Billing.Infrastructure.TaxCatalog;
using Microsoft.EntityFrameworkCore;

namespace Ecunexo.Billing.Infrastructure.Tests.TaxCatalog;

public sealed class TaxCatalogWriterTests
{
    [Fact(DisplayName = "AddTaxRate inserta y omite duplicado")]
    public void AddTaxRate_InsertsAndSkipsDuplicate()
    {
        using var context = CreateContext();
        var writer = new TaxCatalogWriter(context);
        var item = new TaxRateSeedItem("2", "4", "IVA 15%", 15m, new DateOnly(2024, 4, 1));

        var first = writer.AddTaxRate(item);
        var second = writer.AddTaxRate(item);

        Assert.Equal(1, first.Created);
        Assert.Equal(0, first.Skipped);
        Assert.Equal(0, second.Created);
        Assert.Equal(1, second.Skipped);
        Assert.Single(context.TaxRates);
    }

    private static BillingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }
}
