namespace Ecunexo.Billing.Api.Contracts.Catalogs;

public sealed record CatalogSeedResponse(
    int Created,
    int Skipped,
    IReadOnlyList<string> Errors);
