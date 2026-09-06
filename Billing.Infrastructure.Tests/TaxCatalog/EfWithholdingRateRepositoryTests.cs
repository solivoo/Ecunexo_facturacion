using Ecunexo.Billing.Core.TaxCatalog;
using Ecunexo.Billing.Infrastructure.Persistence;
using Ecunexo.Billing.Infrastructure.TaxCatalog.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ecunexo.Billing.Infrastructure.Tests.TaxCatalog;

public sealed class EfWithholdingRateRepositoryTests
{
    [Fact(DisplayName = "GetByRetentionCode retorna porcentaje vigente")]
    public void GetByRetentionCode_WithActiveRate_ReturnsRate()
    {
        using var context = CreateContext();
        context.WithholdingRates.Add(WithholdingRate.Create("REN", "303", "Honorarios", 10m, new DateOnly(2020, 1, 1)));
        context.SaveChanges();

        var repository = new EfWithholdingRateRepository(context);
        var result = repository.GetByRetentionCode("REN", "303", new DateOnly(2024, 1, 1));

        Assert.NotNull(result);
        Assert.Equal(10m, result!.Percentage);
    }

    [Fact(DisplayName = "GetActiveByType solo devuelve vigentes")]
    public void GetActiveByType_FiltersByDate()
    {
        using var context = CreateContext();
        context.WithholdingRates.Add(WithholdingRate.Create("REN", "303", "Honorarios", 10m, new DateOnly(2020, 1, 1)));
        context.WithholdingRates.Add(WithholdingRate.Create("REN", "332", "Pagos al exterior", 25m, new DateOnly(2020, 1, 1), new DateOnly(2021, 1, 1)));
        context.SaveChanges();

        var repository = new EfWithholdingRateRepository(context);
        var result = repository.GetActiveByType("REN", new DateOnly(2024, 1, 1));

        Assert.Single(result);
        Assert.Equal("303", result[0].RetentionCode);
    }

    private static BillingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }
}
