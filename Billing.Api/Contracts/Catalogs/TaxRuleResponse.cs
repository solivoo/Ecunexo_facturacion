namespace Ecunexo.Billing.Api.Contracts.Catalogs;

public sealed record TaxRuleResponse(
    string Code,
    string Description,
    string PayloadJson,
    DateOnly ValidFrom,
    DateOnly? ValidTo);
