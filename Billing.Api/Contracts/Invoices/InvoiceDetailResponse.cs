namespace Ecunexo.Billing.Api.Contracts.Invoices;

public sealed record InvoiceDetailResponse(
    Guid InvoiceId,
    DateOnly IssueDate,
    string Establishment,
    string EmissionPoint,
    string Sequential,
    string? AccessKey,
    string State,
    string? SriTransmissionState,
    decimal SubtotalWithoutTax,
    decimal GrandTotal,
    InvoiceEmitterInfoResponse Emitter,
    InvoiceCounterpartyResponse Counterparty,
    IReadOnlyList<InvoiceDetailLineResponse> Lines,
    IReadOnlyList<InvoiceTaxTotalResponse> TaxTotals,
    bool HasSignedXml,
    bool HasUnsignedXml,
    string PaymentFormCode = "01",
    string? AdditionalNote = null,
    int PaymentTermDays = 0,
    DateTimeOffset? AuthorizationDate = null,
    string? SoftwareProviderRuc = null,
    string? SoftwareProviderName = null,
    string? SoftwareFooterLine = null,
    string DocumentType = "01",
    string? Motivo = null,
    Guid? ModifiedInvoiceId = null,
    string? ModifiedDocumentNumber = null,
    DateOnly? ModifiedIssueDate = null,
    bool CanVoid = false,
    bool IsVoided = false,
    string? VoidPath = null,
    DateOnly? VoidDeadline = null,
    string? VoidMessage = null);

public sealed record InvoiceEmitterInfoResponse(
    string Ruc,
    string BusinessName,
    string? TradeName,
    string MainAddress);

public sealed record InvoiceCounterpartyResponse(
    string IdentificationType,
    string Identification,
    string BusinessName,
    string? Address,
    string? Email = null,
    string? Phone = null);

public sealed record InvoiceDetailLineResponse(
    int LineNumber,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal LineTotalWithoutTax,
    IReadOnlyList<InvoiceDetailLineTaxResponse> Taxes,
    string? MainCode = null,
    Guid? CatalogItemId = null,
    string? ItemKind = null);

public sealed record InvoiceDetailLineTaxResponse(
    string TaxCode,
    string RateCode,
    decimal Rate,
    decimal TaxableBase,
    decimal Value);

public sealed record InvoiceXmlDownloadResponse(
    Guid InvoiceId,
    string? AccessKey,
    string Source,
    string Xml);
