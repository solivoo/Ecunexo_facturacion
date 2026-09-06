namespace Ecunexo.Billing.Core.TaxRules.Ports;

public interface ITaxRuleRepository
{
    TaxRule? GetActive(string code, DateOnly date);

    IReadOnlyList<TaxRule> ListByCode(string code);

    IReadOnlyList<TaxRule> ListAll();
}
