using Ecunexo.Billing.Domain.TaxRules.Ports;

namespace Ecunexo.Billing.Domain.TaxRules;

public sealed class SriVoidPolicy(ITaxRuleRepository taxRuleRepository)
{
    public SriVoidAdvice Advise(DateOnly issueDate, DateOnly asOf, bool isConsumerFinal)
    {
        var row = taxRuleRepository.GetActive(TaxRuleCodes.SriOnlineVoid, asOf);
        var parameters = SriOnlineVoidParameters.FromJson(row?.PayloadJson);
        return SriOnlineVoidEvaluator.Evaluate(issueDate, asOf, isConsumerFinal, parameters);
    }
}
