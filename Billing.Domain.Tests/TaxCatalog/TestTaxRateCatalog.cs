using Ecunexo.Billing.Domain.TaxCatalog;

namespace Ecunexo.Billing.Domain.Tests.TaxCatalog;

internal static class TestTaxRateCatalog
{
    public static InMemoryTaxRateRepository WithIva15()
    {
        var repo = new InMemoryTaxRateRepository();
        repo.Add(TaxRate.Create("2", "4", "IVA 15%", 15m, new DateOnly(2020, 1, 1)));
        return repo;
    }
}