namespace Ecunexo.Billing.Api.Contracts.Catalogs;

public sealed record CreateTaxRateRequest(
    string TaxCode,
    string RateCode,
    string Description,
    decimal Rate,
    DateOnly ValidFrom,
    DateOnly? ValidTo = null);
