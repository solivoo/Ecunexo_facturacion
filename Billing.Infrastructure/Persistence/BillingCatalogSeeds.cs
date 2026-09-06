using Ecunexo.Billing.Core.TaxCatalog.Ports;

namespace Ecunexo.Billing.Infrastructure.Persistence;

public static class BillingCatalogSeeds
{
    /// <summary>Tarifas SRI alineadas con <c>postman/catalogos/tarifas-iva/*.json</c>.</summary>
    public static IReadOnlyList<TaxRateSeedItem> TaxRates { get; } =
    [
        new("2", "0", "IVA 0%", 0m, new DateOnly(2020, 1, 1)),
        new("2", "2", "IVA 12%", 12m, new DateOnly(2020, 1, 1), new DateOnly(2024, 3, 31)),
        new("2", "3", "IVA 14%", 14m, new DateOnly(2020, 1, 1), new DateOnly(2024, 3, 31)),
        new("2", "4", "IVA 15%", 15m, new DateOnly(2024, 4, 1)),
        new("2", "5", "IVA 5%", 5m, new DateOnly(2020, 1, 1)),
        new("2", "6", "IVA no objeto de impuesto", 0m, new DateOnly(2020, 1, 1)),
        new("2", "7", "IVA exento", 0m, new DateOnly(2020, 1, 1)),
        new("3", "0", "ICE 0%", 0m, new DateOnly(2020, 1, 1)),
        new("5", "0", "ISD 0%", 0m, new DateOnly(2020, 1, 1)),
    ];
}
