namespace Ecunexo.Billing.Api.Contracts.Catalogs;

public sealed record CreateWithholdingRateRequest(
    string TaxType,
    string RetentionCode,
    string Description,
    decimal Percentage,
    DateOnly ValidFrom,
    DateOnly? ValidTo = null);
