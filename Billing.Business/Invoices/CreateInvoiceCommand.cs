namespace Ecunexo.Billing.Business.Invoices;

public sealed record CreateInvoiceCommand(
    Guid EmitterId,
    Guid? TenantId,
    Guid? CreatedByUserId,
    string? Establishment,
    string? EmissionPoint,
    DateOnly IssueDate,
    CreateInvoiceCounterparty Counterparty,
    IReadOnlyList<CreateInvoiceLine> Lines,
    string? PaymentFormCode,
    string? AdditionalNote,
    int PaymentTermDays);

public sealed record CreateInvoiceCounterparty(
    string IdentificationType,
    string Identification,
    string BusinessName,
    string? Address,
    string? Email,
    string? Phone);

public sealed record CreateInvoiceLine(
    int LineNumber,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal LineTotalWithoutTax,
    IReadOnlyList<CreateInvoiceLineTax> Taxes,
    string? MainCode,
    Guid? CatalogItemId,
    string? ItemKind);

public sealed record CreateInvoiceLineTax(
    string TaxCode,
    string RateCode,
    decimal Rate,
    decimal TaxableBase,
    decimal Value);
