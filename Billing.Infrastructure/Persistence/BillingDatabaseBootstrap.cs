using Ecunexo.Billing.Core.TaxCatalog.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecunexo.Billing.Infrastructure.Persistence;

public static class BillingDatabaseBootstrap
{
    public static async Task<(int Created, int Skipped)> ApplyAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<BillingDatabaseBootstrapOptions>>()
            .Value;
        if (!options.Enabled)
            return (0, 0);

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("BillingDatabaseBootstrap");

        if (options.Migrate)
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            logger.LogInformation("Aplicando migraciones pendientes de billing...");
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (!options.SeedCatalogs)
            return (0, 0);

        var writer = scope.ServiceProvider.GetRequiredService<ITaxCatalogWriter>();
        var (created, skipped) = SeedTaxRates(writer);

        if (created > 0 || skipped > 0)
        {
            logger.LogInformation(
                "Semilla de tarifas IVA: {Created} creadas, {Skipped} omitidas (ya existían).",
                created,
                skipped);
        }

        return (created, skipped);
    }

    internal static (int Created, int Skipped) SeedTaxRates(ITaxCatalogWriter writer)
    {
        var created = 0;
        var skipped = 0;

        foreach (var item in BillingCatalogSeeds.TaxRates)
        {
            var result = writer.AddTaxRate(item);
            created += result.Created;
            skipped += result.Skipped;
        }

        return (created, skipped);
    }
}
