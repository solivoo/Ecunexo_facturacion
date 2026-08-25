namespace Ecunexo.Billing.Infrastructure.Inventory;

public sealed class InventoryEgressOptions
{
    public const string SectionName = "InventoryEgress";

    public bool Enabled { get; set; }

    /// <summary>Base URL de ecunexo_api, p. ej. http://localhost:5088</summary>
    public string? BaseUrl { get; set; }

    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 30;
}

public sealed record InventoryEgressLineDto(
    Guid CatalogItemId,
    decimal Quantity,
    string? ItemKind,
    string? Description);

public sealed record InventoryEgressRequestDto(
    Guid BillingInvoiceId,
    IReadOnlyList<InventoryEgressLineDto> Lines);

public interface IInventoryEgressNotifier
{
    Task NotifyAuthorizedAsync(
        Guid tenantId,
        Guid billingInvoiceId,
        IReadOnlyList<InventoryEgressLineDto> lines,
        CancellationToken cancellationToken = default);
}
