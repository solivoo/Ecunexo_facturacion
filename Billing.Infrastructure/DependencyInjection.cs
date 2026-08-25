using Ecunexo.Billing.Domain.Documents.Ports;
using Ecunexo.Billing.Domain.Emitter.Ports;
using Ecunexo.Billing.Domain.Sri.Policies;
using Ecunexo.Billing.Domain.Sri.Ports;
using Ecunexo.Billing.Domain.TaxCatalog.Ports;
using Ecunexo.Billing.Domain.TaxRules;
using Ecunexo.Billing.Domain.TaxRules.Ports;
using Ecunexo.Billing.Infrastructure.Persistence;
using Ecunexo.Billing.Infrastructure.Persistence.Repositories;
using Ecunexo.Billing.Infrastructure.Secrets.Infisical;
using Ecunexo.Billing.Infrastructure.Sri;
using Ecunexo.Billing.Infrastructure.Sri.Adapters;
using Ecunexo.Billing.Infrastructure.Sri.Workers;
using Ecunexo.Billing.Infrastructure.TaxCatalog;
using Ecunexo.Billing.Infrastructure.TaxCatalog.Repositories;
using Ecunexo.Billing.Infrastructure.TaxRules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecunexo.Billing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingInfrastructure(
        this IServiceCollection services,
        string postgresConnectionString,
        IConfiguration? configuration = null)
    {
        if (string.IsNullOrWhiteSpace(postgresConnectionString))
            throw new ArgumentException("La cadena de conexión no puede estar vacía.");

        services.AddDbContext<BillingDbContext>(options =>
            options.UseNpgsql(
                postgresConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    BillingPersistence.MigrationsHistoryTable,
                    BillingPersistence.Schema)));

        services.AddScoped<ITaxRateRepository, EfTaxRateRepository>();
        services.AddScoped<IWithholdingRateRepository, EfWithholdingRateRepository>();
        services.AddScoped<ITaxCatalogWriter, TaxCatalogWriter>();
        services.AddScoped<ITaxRuleRepository, EfTaxRuleRepository>();
        services.AddScoped<SriVoidPolicy>();
        services.AddScoped<IEmitterRepository, EfEmitterRepository>();
        services.AddScoped<IInvoiceRepository, EfInvoiceRepository>();
        services.AddScoped<ISriOutboxRepository, EfSriOutboxRepository>();
        services.AddSingleton<IOfflineEmissionPolicy, OfflineEmissionPolicy>();
        services.AddSingleton<SriCircuitBreaker>();

        if (configuration is not null)
        {
            services.AddInfisicalSecrets(configuration);
            services.AddSriGateway(configuration);
            services.AddInventoryEgressNotifier(configuration);
            services.AddHostedService<SriOutboxWorker>();
        }

        return services;
    }

    public static IServiceCollection AddInventoryEgressNotifier(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<Inventory.InventoryEgressOptions>(
            configuration.GetSection(Inventory.InventoryEgressOptions.SectionName));
        var timeoutSeconds = configuration.GetValue(
            $"{Inventory.InventoryEgressOptions.SectionName}:TimeoutSeconds",
            30);
        var baseUrl = configuration[$"{Inventory.InventoryEgressOptions.SectionName}:BaseUrl"];

        services.AddHttpClient(Inventory.HttpInventoryEgressNotifier.HttpClientName, client =>
        {
            if (!string.IsNullOrWhiteSpace(baseUrl))
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 120));
        });
        services.AddScoped<Inventory.IInventoryEgressNotifier, Inventory.HttpInventoryEgressNotifier>();
        return services;
    }

    public static IServiceCollection AddSriGateway(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SriOptions>(configuration.GetSection(SriOptions.SectionName));
        services.AddSingleton<ISriEmissionIdentityResolver, SriEmissionIdentityResolver>();
        var useInMemory = configuration.GetValue($"{SriOptions.SectionName}:UseInMemoryGateway", true);
        var timeoutSeconds = configuration.GetValue($"{SriOptions.SectionName}:HttpTimeoutSeconds", 60);

        if (useInMemory)
        {
            services.AddSingleton<ISriGateway, InMemorySriGateway>();
            return services;
        }

        services.AddHttpClient(SoapSriGateway.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 10, 180));
            client.DefaultRequestHeaders.ExpectContinue = false;
        });
        services.AddSingleton<ISriGateway, SoapSriGateway>();
        return services;
    }

    public static IServiceCollection AddInfisicalSecrets(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<InfisicalOptions>(configuration.GetSection(InfisicalOptions.SectionName));
        services.AddMemoryCache();

        services.AddHttpClient<InfisicalClient>((_, client) =>
        {
            var baseUrl = configuration[$"{InfisicalOptions.SectionName}:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Infisical:BaseUrl no está configurado.");

            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<InfisicalSigningCertificateProvider>();
        services.AddSingleton<ISigningCertificateProvider>(sp =>
            sp.GetRequiredService<InfisicalSigningCertificateProvider>());
        services.AddSingleton<ISigningPkcs12MaterialProvider>(sp =>
            sp.GetRequiredService<InfisicalSigningCertificateProvider>());

        return services;
    }
}
