using Ecunexo.Billing.Core.TaxCatalog;
using Ecunexo.Billing.Core.TaxCatalog.Ports;
using Ecunexo.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecunexo.Billing.Infrastructure.TaxCatalog.Repositories;

public sealed class EfTaxRateRepository(BillingDbContext dbContext) : ITaxRateRepository
{
    public TaxRate? GetByRateCodes(string taxCode, string rateCode, DateOnly date) =>
        dbContext.TaxRates
            .AsNoTracking()
            .FirstOrDefault(x =>
                x.TaxCode == taxCode &&
                x.RateCode == rateCode &&
                x.ValidFrom <= date &&
                (x.ValidTo == null || x.ValidTo >= date));

    public IReadOnlyList<TaxRate> GetActiveByTaxCode(string taxCode, DateOnly date) =>
        dbContext.TaxRates
            .AsNoTracking()
            .Where(x =>
                x.TaxCode == taxCode &&
                x.ValidFrom <= date &&
                (x.ValidTo == null || x.ValidTo >= date))
            .ToList();
}
