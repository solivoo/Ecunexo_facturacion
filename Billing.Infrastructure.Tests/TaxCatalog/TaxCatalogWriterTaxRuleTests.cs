using Ecunexo.Billing.Domain.TaxCatalog.Ports;
using Ecunexo.Billing.Domain.TaxRules;
using Ecunexo.Billing.Infrastructure.Persistence;
using Ecunexo.Billing.Infrastructure.TaxCatalog;
using Microsoft.EntityFrameworkCore;

namespace Ecunexo.Billing.Infrastructure.Tests.TaxCatalog;

public sealed class TaxCatalogWriterTaxRuleTests
{
    [Fact(DisplayName = "Nueva vigencia cierra la regla abierta el día anterior")]
    public void AddSriOnlineVoidRule_ClosesPreviousOpenRule()
    {
        using var context = CreateContext();
        context.TaxRules.Add(TaxRule.Create(
            TaxRuleCodes.SriOnlineVoid,
            "Día 7",
            SriOnlineVoidParameters.CurrentLaw.ToJson(),
            new DateOnly(2025, 8, 1)));
        context.SaveChanges();

        var writer = new TaxCatalogWriter(context);
        var result = writer.AddSriOnlineVoidRule(new SriOnlineVoidRuleSeedItem(
            "Día 15",
            new DateOnly(2027, 1, 1),
            15));

        Assert.Equal(1, result.Created);
        Assert.Empty(result.Errors);

        var previous = context.TaxRules.Single(x => x.ValidFrom == new DateOnly(2025, 8, 1));
        var current = context.TaxRules.Single(x => x.ValidFrom == new DateOnly(2027, 1, 1));
        Assert.Equal(new DateOnly(2026, 12, 31), previous.ValidTo);
        Assert.Null(current.ValidTo);
        Assert.Equal(15, SriOnlineVoidParameters.FromJson(current.PayloadJson).DeadlineDayOfFollowingMonth);
    }

    [Fact(DisplayName = "Misma fecha de inicio no pisa el historial")]
    public void AddSriOnlineVoidRule_SameValidFrom_Skips()
    {
        using var context = CreateContext();
        var writer = new TaxCatalogWriter(context);
        writer.AddSriOnlineVoidRule(new SriOnlineVoidRuleSeedItem(
            "Día 7",
            new DateOnly(2025, 8, 1),
            7));

        var skipped = writer.AddSriOnlineVoidRule(new SriOnlineVoidRuleSeedItem(
            "Otra",
            new DateOnly(2025, 8, 1),
            10));

        Assert.Equal(0, skipped.Created);
        Assert.Equal(1, skipped.Skipped);
        Assert.Equal(1, context.TaxRules.Count());
    }

    [Fact(DisplayName = "No permite rebobinar ValidFrom")]
    public void AddSriOnlineVoidRule_EarlierValidFrom_Fails()
    {
        using var context = CreateContext();
        var writer = new TaxCatalogWriter(context);
        writer.AddSriOnlineVoidRule(new SriOnlineVoidRuleSeedItem(
            "Día 7",
            new DateOnly(2025, 8, 1),
            7));

        var result = writer.AddSriOnlineVoidRule(new SriOnlineVoidRuleSeedItem(
            "Anterior",
            new DateOnly(2024, 1, 1),
            10));

        Assert.Equal(0, result.Created);
        Assert.NotEmpty(result.Errors);
    }

    private static BillingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }
}
