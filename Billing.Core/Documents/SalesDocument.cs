namespace Ecunexo.Billing.Core.Documents;

public abstract class SalesDocument : ElectronicDocument
{
    public Counterparty Counterparty { get; protected set; } = null!;
    public Money SubtotalWithoutTax { get; protected set; } = null!;
    public IReadOnlyList<DocumentTaxTotal> TaxTotals { get; protected set; } = [];
    public Money GrandTotal { get; protected set; } = null!;
    /// <summary>Código SRI forma de pago (2 dígitos). Por defecto 01.</summary>
    public string PaymentFormCode { get; protected set; } = "01";
    public IReadOnlyList<InvoiceLine> Lines { get; protected set; } = [];
    public string? AdditionalNote { get; protected set; }

    protected SalesDocument() { }
}