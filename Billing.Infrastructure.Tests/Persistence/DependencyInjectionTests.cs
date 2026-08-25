using Ecunexo.Billing.Domain.TaxCatalog.Ports;
using Ecunexo.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecunexo.Billing.Infrastructure.Tests.Persistence;

public sealed class DependencyInjectionTests
{
    [Fact(DisplayName = "AddBillingInfrastructure registra DbContext y repositorios")]
    public void AddBillingInfrastructure_RegistersServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Port=5432;Database=ecunexo;Username=postgres;Password=root"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddBillingInfrastructure(configuration.GetConnectionString("Default")!);

        var provider = services.BuildServiceProvider();

        var dbContext = provider.GetService<BillingDbContext>();
        var taxRepository = provider.GetService<ITaxRateRepository>();
        var withholdRepository = provider.GetService<IWithholdingRateRepository>();

        Assert.NotNull(dbContext);
        Assert.NotNull(taxRepository);
        Assert.NotNull(withholdRepository);
    }
}
