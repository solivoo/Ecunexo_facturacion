using Ecunexo.Billing.Core.TaxCatalog;
using Ecunexo.Billing.Core.TaxCatalog.Ports;

namespace Ecunexo.Billing.Api.Tests.Support;

internal sealed class InMemoryTaxRateRepository : ITaxRateRepository
{
    private readonly List<TaxRate> _rates = [];

    public InMemoryTaxRateRepository()
    {
        _rates.Add(TaxRate.Create("2", "4", "IVA 15%", 15m, new DateOnly(2020, 1, 1)));
    }

    public TaxRate? GetByRateCodes(string taxCode, string rateCode, DateOnly date) =>
        _rates.FirstOrDefault(r => r.TaxCode == taxCode && r.RateCode == rateCode && r.IsActiveOn(date));

    public IReadOnlyList<TaxRate> GetActiveByTaxCode(string taxCode, DateOnly date) =>
        _rates.Where(r => r.TaxCode == taxCode && r.IsActiveOn(date)).ToList();
}
