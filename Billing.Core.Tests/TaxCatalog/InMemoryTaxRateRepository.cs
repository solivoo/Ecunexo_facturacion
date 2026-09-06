using Ecunexo.Billing.Core.TaxCatalog;
using Ecunexo.Billing.Core.TaxCatalog.Ports;

namespace Ecunexo.Billing.Core.Tests.TaxCatalog;

public sealed class InMemoryTaxRateRepository : ITaxRateRepository
{
    private readonly List<TaxRate> _rates = [];

    public void Add(TaxRate rate) => _rates.Add(rate);

    public TaxRate? GetByRateCodes(string taxCode, string rateCode, DateOnly date) =>
        _rates.FirstOrDefault(r =>
            r.TaxCode == taxCode &&
            r.RateCode == rateCode &&
            r.IsActiveOn(date));

    public IReadOnlyList<TaxRate> GetActiveByTaxCode(string taxCode, DateOnly date) =>
        _rates.Where(r => r.TaxCode == taxCode && r.IsActiveOn(date)).ToList();
}