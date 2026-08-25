namespace Ecunexo.Billing.Api.Contracts.Catalogs;

public sealed record CreateTaxRuleRequest(
    string Description,
    DateOnly ValidFrom,
    int DeadlineDayOfFollowingMonth,
    bool ExtendToNextWeekday = true,
    bool ConsumerFinalCannotVoid = true,
    bool ConsumerFinalCannotCreditNote = true);
