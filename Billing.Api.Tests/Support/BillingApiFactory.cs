using Ecunexo.Billing.Core.Emitter.Ports;
using Ecunexo.Billing.Core.Sri.Ports;
using Ecunexo.Billing.Core.TaxCatalog.Ports;
using Ecunexo.Billing.Core.TaxRules.Ports;
using Ecunexo.Billing.Infrastructure.Sri.Adapters;
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
                ["ConnectionStrings:Default"] = "Host=localhost;Port=5432;Database=ecunexo;Username=postgres;Password=root",
                ["Billing:DatabaseBootstrap:Enabled"] = "false",
                ["Infisical:BaseUrl"] = "http://localhost:8080",
                ["Sri:UseInMemoryGateway"] = "true",
                ["Sri:AuthorizationPollDelaySeconds"] = "0",
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

            services.RemoveAll<ISriGateway>();
            services.AddSingleton<ISriGateway, InMemorySriGateway>();

            services.RemoveAll<IElectronicSignatureService>();
            services.AddSingleton<IElectronicSignatureService, DummySignatureService>();
        });
    }

    private sealed class DummySignatureService : IElectronicSignatureService
    {
        public Task<byte[]> SignXmlAsync(Guid emitterId, byte[] xml, CancellationToken cancellationToken = default) =>
            Task.FromResult(xml);
    }
}
