using Ecunexo.Billing.Core.TaxRules;
using Ecunexo.Billing.Core.TaxRules.Ports;

namespace Ecunexo.Billing.Api.Tests.Support;

internal sealed class InMemoryTaxRuleRepository : ITaxRuleRepository
{
    public TaxRule? GetActive(string code, DateOnly date) => null;

    public IReadOnlyList<TaxRule> ListByCode(string code) => [];

    public IReadOnlyList<TaxRule> ListAll() => [];
}
