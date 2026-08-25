namespace Ecunexo.Billing.Infrastructure.Persistence.Entities;

public sealed class InvoiceLineEntity
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public int LineNumber { get; set; }
    public string? MainCode { get; set; }
    public Guid? CatalogItemId { get; set; }
    public string? ItemKind { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotalWithoutTax { get; set; }
    public string TaxesJson { get; set; } = "[]";

    public ElectronicInvoiceEntity Invoice { get; set; } = null!;
}
