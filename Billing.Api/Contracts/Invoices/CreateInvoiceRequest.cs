namespace Ecunexo.Billing.Api.Contracts.Invoices;

public sealed record CreateInvoiceRequest(
    string EmitterRuc,
    string Establishment,
    string EmissionPoint,
    string Sequential,
    DateOnly IssueDate,
    CounterpartyRequest Counterparty,
    IReadOnlyList<InvoiceLineRequest> Lines,
    string? PaymentFormCode = null,
    string? AdditionalNote = null,
    int PaymentTermDays = 0);

public sealed record CounterpartyRequest(
    string IdentificationType,
    string Identification,
    string BusinessName,
    string? Address,
    string? Email = null,
    string? Phone = null);

public sealed record InvoiceLineRequest(
    int LineNumber,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal LineTotalWithoutTax,
    IReadOnlyList<LineTaxRequest> Taxes,
    string? MainCode = null,
    Guid? CatalogItemId = null,
    string? ItemKind = null);

public sealed record LineTaxRequest(
    string TaxCode,
    string RateCode,
    decimal Rate,
    decimal TaxableBase,
    decimal Value);
