namespace Ecunexo.Billing.Api.Contracts.Catalogs;

public sealed record WithholdingRateResponse(
    string TaxType,
    string RetentionCode,
    string Description,
    decimal Percentage,
    DateOnly ValidFrom,
    DateOnly? ValidTo);
