namespace Ecunexo.Billing.Domain.Documents;
using Ecunexo.Billing.Domain.Documents.Services;
using Ecunexo.Billing.Domain;

public class InvoiceLine
{
    public int LineNumber { get; }
    public string Description { get; }
    public decimal Quantity { get; }
    public Money UnitPrice { get; }
    public Money Discount { get; }
    public Money LineTotalWithoutTax { get; }
    public IReadOnlyList<LineTax> Taxes { get; }

    /// <summary>Código principal SRI (SKU), máximo 25 caracteres.</summary>
    public string? MainCode { get; }

    /// <summary>Referencia al catálogo tenant (sin FK cross-host).</summary>
    public Guid? CatalogItemId { get; }

    /// <summary><c>physical</c> o <c>service</c>; null = legacy sin snapshot.</summary>
    public string? ItemKind { get; }

    public InvoiceLine(
        int lineNumber,
        string description,
        decimal quantity,
        Money unitPrice,
        Money discount,
        Money lineTotalWithoutTax,
        IReadOnlyList<LineTax> taxes,
        string? mainCode = null,
        Guid? catalogItemId = null,
        string? itemKind = null)
    {
        LineNumber = lineNumber;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Discount = discount;
        LineTotalWithoutTax = lineTotalWithoutTax;
        Taxes = taxes;
        MainCode = string.IsNullOrWhiteSpace(mainCode) ? null : mainCode.Trim();
        CatalogItemId = catalogItemId == Guid.Empty ? null : catalogItemId;
        ItemKind = NormalizeItemKind(itemKind);

        InvoiceLineValidator.Validate(this);
    }

    private static string? NormalizeItemKind(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var kind = raw.Trim().ToLowerInvariant();
        return kind is "physical" or "service" ? kind : null;
    }
}
