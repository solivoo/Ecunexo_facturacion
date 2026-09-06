using Ecunexo.Billing.Core.TaxCatalog;
using Ecunexo.Billing.Core.TaxCatalog.Ports;
using Ecunexo.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecunexo.Billing.Infrastructure.TaxCatalog.Repositories;

public sealed class EfWithholdingRateRepository(BillingDbContext dbContext) : IWithholdingRateRepository
{
    public WithholdingRate? GetByRetentionCode(string taxType, string retentionCode, DateOnly date) =>
        dbContext.WithholdingRates
            .AsNoTracking()
            .FirstOrDefault(x =>
                x.TaxType == taxType &&
                x.RetentionCode == retentionCode &&
                x.ValidFrom <= date &&
                (x.ValidTo == null || x.ValidTo >= date));

    public IReadOnlyList<WithholdingRate> GetActiveByType(string taxType, DateOnly date) =>
        dbContext.WithholdingRates
            .AsNoTracking()
            .Where(x =>
                x.TaxType == taxType &&
                x.ValidFrom <= date &&
                (x.ValidTo == null || x.ValidTo >= date))
            .ToList();
}
