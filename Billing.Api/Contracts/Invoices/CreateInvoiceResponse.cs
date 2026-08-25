namespace Ecunexo.Billing.Api.Contracts.Invoices;

public sealed record CreateInvoiceResponse(
    Guid InvoiceId,
    string State,
    decimal SubtotalWithoutTax,
    decimal GrandTotal,
    IReadOnlyList<InvoiceTaxTotalResponse> TaxTotals);

public sealed record InvoiceTaxTotalResponse(
    string TaxCode,
    string RateCode,
    decimal TaxableBase,
    decimal Value);
