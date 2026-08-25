using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Emitter;
using Ecunexo.Billing.Domain.Sri;
using Ecunexo.Billing.Infrastructure.Sri;
using Microsoft.Extensions.Options;

namespace Ecunexo.Billing.Infrastructure.Tests.Sri;

public class SriEmissionIdentityResolverTests
{
    [Fact(DisplayName = "Test: tenant inventado usa RUC inscrito en celcer")]
    public void Resolve_Test_SubstitutesEnrolledRuc()
    {
        var resolver = CreateResolver(SriEnvironment.Test, configured: true);
        var andes = Emitter.Create(
            Ruc.Create("1792146736001"),
            "Andes Retail Cía. Ltda.",
            "Av. Amazonas N24-03 y Colón, Quito",
            "Andes Retail");

        var identity = resolver.Resolve(andes, "001", "001");

        Assert.True(identity.IsSubstituted);
        Assert.Equal("0926398074001", identity.Ruc.Value);
        Assert.Equal("Ecunexo S.A", identity.BusinessName);
        Assert.Equal("002", identity.Establishment?.Value);
        Assert.Equal("001", identity.EmissionPoint?.Value);
    }

    [Fact(DisplayName = "Test: el mismo RUC inscrito no se sustituye")]
    public void Resolve_Test_SameRuc_DoesNotSubstitute()
    {
        var resolver = CreateResolver(SriEnvironment.Test, configured: true);
        var house = Emitter.Create(
            Ruc.Create("0926398074001"),
            "Ecunexo S.A",
            "Guayaquil via Daule");

        var identity = resolver.Resolve(house, "002", "001");

        Assert.False(identity.IsSubstituted);
        Assert.Equal("0926398074001", identity.Ruc.Value);
        Assert.Equal("002", identity.Establishment?.Value);
    }

    [Fact(DisplayName = "Producción: nunca sustituye el RUC del tenant")]
    public void Resolve_Production_KeepsTenantRuc()
    {
        var resolver = CreateResolver(SriEnvironment.Production, configured: true);
        var andes = Emitter.Create(
            Ruc.Create("1792146736001"),
            "Andes Retail Cía. Ltda.",
            "Quito");

        var identity = resolver.Resolve(andes, "001", "001");

        Assert.False(identity.IsSubstituted);
        Assert.Equal("1792146736001", identity.Ruc.Value);
    }

    [Fact(DisplayName = "Test sin RUC configurado: no sustituye")]
    public void Resolve_Test_NotConfigured_KeepsTenantRuc()
    {
        var resolver = CreateResolver(SriEnvironment.Test, configured: false);
        var andes = Emitter.Create(
            Ruc.Create("1792146736001"),
            "Andes Retail Cía. Ltda.",
            "Quito");

        var identity = resolver.Resolve(andes, "001", "001");

        Assert.False(identity.IsSubstituted);
        Assert.Equal("1792146736001", identity.Ruc.Value);
    }

    private static SriEmissionIdentityResolver CreateResolver(SriEnvironment environment, bool configured)
    {
        var options = new SriOptions
        {
            DefaultEnvironment = environment.ToString(),
            TestEmission = configured
                ? new SriTestEmissionOptions
                {
                    Ruc = "0926398074001",
                    BusinessName = "Ecunexo S.A",
                    MainAddress = "Guayaquil via Daule",
                    TradeName = "EcuNexo",
                    Establishment = "002",
                    EmissionPoint = "001",
                }
                : new SriTestEmissionOptions(),
        };
        return new SriEmissionIdentityResolver(Options.Create(options));
    }
}
