using Ecunexo.Billing.Domain.TaxCatalog.Ports;
using Ecunexo.Billing.Domain.TaxRules.Ports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ecunexo.Billing.Api.Tests.Support;

public sealed class BillingApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sri:UseInMemoryGateway"] = "true",
                ["Sri:TestEmission:Ruc"] = "",
                ["Sri:TestEmission:BusinessName"] = "",
                ["Sri:TestEmission:MainAddress"] = "",
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ITaxRateRepository>();
            services.RemoveAll<IWithholdingRateRepository>();
            services.RemoveAll<ITaxRuleRepository>();

            services.AddSingleton<ITaxRateRepository, InMemoryTaxRateRepository>();
            services.AddSingleton<IWithholdingRateRepository, InMemoryWithholdingRateRepository>();
            services.AddSingleton<ITaxRuleRepository, InMemoryTaxRuleRepository>();
        });
    }
}
