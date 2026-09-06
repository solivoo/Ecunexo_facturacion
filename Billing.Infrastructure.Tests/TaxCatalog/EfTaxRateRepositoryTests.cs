using Ecunexo.Billing.Core.TaxCatalog;
using Ecunexo.Billing.Infrastructure.Persistence;
using Ecunexo.Billing.Infrastructure.TaxCatalog.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ecunexo.Billing.Infrastructure.Tests.TaxCatalog;

public sealed class EfTaxRateRepositoryTests
{
    [Fact(DisplayName = "GetByRateCodes retorna tarifa vigente")]
    public void GetByRateCodes_WithActiveRate_ReturnsRate()
    {
        using var context = CreateContext();
        context.TaxRates.Add(TaxRate.Create("2", "4", "IVA 15%", 15m, new DateOnly(2020, 1, 1)));
        context.SaveChanges();

        var repository = new EfTaxRateRepository(context);
        var result = repository.GetByRateCodes("2", "4", new DateOnly(2024, 1, 1));

        Assert.NotNull(result);
        Assert.Equal(15m, result!.Rate);
    }

    [Fact(DisplayName = "GetByRateCodes ignora tarifa fuera de vigencia")]
    public void GetByRateCodes_WithExpiredRate_ReturnsNull()
    {
        using var context = CreateContext();
        context.TaxRates.Add(TaxRate.Create("2", "4", "IVA temporal", 15m, new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31)));
        context.SaveChanges();

        var repository = new EfTaxRateRepository(context);
        var result = repository.GetByRateCodes("2", "4", new DateOnly(2024, 1, 1));

        Assert.Null(result);
    }

    private static BillingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }
}
