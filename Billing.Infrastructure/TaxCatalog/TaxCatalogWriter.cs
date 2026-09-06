using Ecunexo.Billing.Core.TaxCatalog;
using Ecunexo.Billing.Core.TaxCatalog.Ports;
using Ecunexo.Billing.Core.TaxRules;
using Ecunexo.Billing.Infrastructure.Persistence;

namespace Ecunexo.Billing.Infrastructure.TaxCatalog;

public sealed class TaxCatalogWriter(BillingDbContext dbContext) : ITaxCatalogWriter
{
    public CatalogWriteResult AddTaxRate(TaxRateSeedItem item) =>
        AddItem(
            () => TaxRate.Create(item.TaxCode, item.RateCode, item.Description, item.Rate, item.ValidFrom, item.ValidTo),
            () => dbContext.TaxRates.Any(x =>
                x.TaxCode == item.TaxCode &&
                x.RateCode == item.RateCode &&
                x.ValidFrom == item.ValidFrom),
            entity => dbContext.TaxRates.Add(entity));

    public CatalogWriteResult AddWithholdingRate(WithholdingRateSeedItem item) =>
        AddItem(
            () => WithholdingRate.Create(
                item.TaxType,
                item.RetentionCode,
                item.Description,
                item.Percentage,
                item.ValidFrom,
                item.ValidTo),
            () => dbContext.WithholdingRates.Any(x =>
                x.TaxType == item.TaxType &&
                x.RetentionCode == item.RetentionCode &&
                x.ValidFrom == item.ValidFrom),
            entity => dbContext.WithholdingRates.Add(entity));

    public CatalogWriteResult AddSriOnlineVoidRule(SriOnlineVoidRuleSeedItem item)
    {
        try
        {
            var parameters = SriOnlineVoidParameters.Create(
                item.DeadlineDayOfFollowingMonth,
                item.ExtendToNextWeekday,
                item.ConsumerFinalCannotVoid,
                item.ConsumerFinalCannotCreditNote);
            var code = TaxRuleCodes.SriOnlineVoid;

            if (dbContext.TaxRules.Any(x => x.Code == code && x.ValidFrom == item.ValidFrom))
                return new CatalogWriteResult(0, 1, []);

            var latest = dbContext.TaxRules
                .Where(x => x.Code == code)
                .OrderByDescending(x => x.ValidFrom)
                .FirstOrDefault();

            if (latest is not null && item.ValidFrom <= latest.ValidFrom)
            {
                return new CatalogWriteResult(
                    0,
                    0,
                    ["La nueva vigencia debe ser posterior a la última registrada."]);
            }

            if (latest is not null && latest.ValidTo is null)
                latest.Deactivate(item.ValidFrom.AddDays(-1));
            else if (latest is not null && latest.ValidTo >= item.ValidFrom)
            {
                return new CatalogWriteResult(
                    0,
                    0,
                    ["La nueva vigencia se solapa con una regla ya cerrada."]);
            }

            dbContext.TaxRules.Add(TaxRule.Create(
                code,
                item.Description,
                parameters.ToJson(),
                item.ValidFrom));
            dbContext.SaveChanges();
            return new CatalogWriteResult(1, 0, []);
        }
        catch (Exception ex)
        {
            return new CatalogWriteResult(0, 0, [ex.Message]);
        }
    }

    private CatalogWriteResult AddItem<TEntity>(
        Func<TEntity> create,
        Func<bool> exists,
        Action<TEntity> add)
        where TEntity : class
    {
        try
        {
            if (exists())
                return new CatalogWriteResult(0, 1, []);

            var entity = create();
            add(entity);
            dbContext.SaveChanges();
            return new CatalogWriteResult(1, 0, []);
        }
        catch (Exception ex)
        {
            return new CatalogWriteResult(0, 0, [ex.Message]);
        }
    }
}
