using Ecunexo.Billing.Core.TaxCatalog.Ports;
using Ecunexo.Billing.Infrastructure.Persistence;
using Ecunexo.Billing.Infrastructure.TaxCatalog;
using Ecunexo.Billing.Infrastructure.TaxCatalog.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecunexo.Billing.Infrastructure.Tests.Persistence;

public sealed class BillingDatabaseBootstrapTests
{
    [Fact(DisplayName = "SeedTaxRates inserta las 9 tarifas base")]
    public void SeedTaxRates_InsertsAllBaseRates()
    {
        using var scope = BuildServices(seedCatalogs: true).CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<ITaxCatalogWriter>();

        var (created, skipped) = BillingDatabaseBootstrap.SeedTaxRates(writer);

        Assert.Equal(9, created);
        Assert.Equal(0, skipped);
        Assert.Equal(9, scope.ServiceProvider.GetRequiredService<BillingDbContext>().TaxRates.Count());
    }

    [Fact(DisplayName = "SeedTaxRates es idempotente")]
    public void SeedTaxRates_IsIdempotent()
    {
        using var scope = BuildServices(seedCatalogs: true).CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<ITaxCatalogWriter>();

        BillingDatabaseBootstrap.SeedTaxRates(writer);
        var (created, skipped) = BillingDatabaseBootstrap.SeedTaxRates(writer);

        Assert.Equal(0, created);
        Assert.Equal(9, skipped);
        Assert.Equal(9, scope.ServiceProvider.GetRequiredService<BillingDbContext>().TaxRates.Count());
    }

    [Fact(DisplayName = "IVA 15% queda vigente para emisión actual")]
    public void SeedTaxRates_IncludesActiveIva15()
    {
        using var scope = BuildServices(seedCatalogs: true).CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<ITaxCatalogWriter>();
        BillingDatabaseBootstrap.SeedTaxRates(writer);

        var rate = scope.ServiceProvider
            .GetRequiredService<ITaxRateRepository>()
            .GetByRateCodes("2", "4", new DateOnly(2026, 8, 29));

        Assert.NotNull(rate);
        Assert.Equal(15m, rate!.Rate);
    }

    [Fact(DisplayName = "Bootstrap omitido cuando SeedCatalogs es false")]
    public async Task ApplyAsync_SkipsSeedWhenDisabled()
    {
        var services = BuildServices(seedCatalogs: false);

        await BillingDatabaseBootstrap.ApplyAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        Assert.Empty(db.TaxRates);
    }

    private static ServiceProvider BuildServices(bool seedCatalogs)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<BillingDatabaseBootstrapOptions>()
            .Configure(options =>
            {
                options.Enabled = true;
                options.Migrate = false;
                options.SeedCatalogs = seedCatalogs;
            });

        services.AddDbContext<BillingDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<ITaxCatalogWriter, TaxCatalogWriter>();
        services.AddScoped<ITaxRateRepository, EfTaxRateRepository>();

        return services.BuildServiceProvider();
    }
}
