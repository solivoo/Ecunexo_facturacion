using Ecunexo.Billing.Domain.TaxRules;
using Ecunexo.Billing.Domain.TaxRules.Ports;
using Ecunexo.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecunexo.Billing.Infrastructure.TaxRules;

public sealed class EfTaxRuleRepository(BillingDbContext db) : ITaxRuleRepository
{
    public TaxRule? GetActive(string code, DateOnly date)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalized = code.Trim().ToLowerInvariant();
        return db.TaxRules.AsNoTracking()
            .Where(x =>
                x.Code == normalized
                && x.ValidFrom <= date
                && (x.ValidTo == null || x.ValidTo >= date))
            .OrderByDescending(x => x.ValidFrom)
            .FirstOrDefault();
    }

    public IReadOnlyList<TaxRule> ListByCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalized = code.Trim().ToLowerInvariant();
        return db.TaxRules.AsNoTracking()
            .Where(x => x.Code == normalized)
            .OrderBy(x => x.ValidFrom)
            .ToList();
    }

    public IReadOnlyList<TaxRule> ListAll() =>
        db.TaxRules.AsNoTracking()
            .OrderBy(x => x.Code)
            .ThenBy(x => x.ValidFrom)
            .ToList();
}
