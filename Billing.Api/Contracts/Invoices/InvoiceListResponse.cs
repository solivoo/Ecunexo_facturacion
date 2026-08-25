namespace Ecunexo.Billing.Api.Contracts.Invoices;

public sealed record InvoiceListResponse(
    IReadOnlyList<InvoiceListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record InvoiceListItemResponse(
    Guid InvoiceId,
    DateOnly IssueDate,
    string Establishment,
    string EmissionPoint,
    string Sequential,
    string? AccessKey,
    string CounterpartyName,
    string CounterpartyIdentification,
    decimal GrandTotal,
    string State,
    string? SriTransmissionState,
    DateTimeOffset CreatedAt,
    bool CanResend,
    string DocumentType = "01",
    bool CanVoid = false,
    bool IsVoided = false,
    Guid? ModifiedInvoiceId = null,
    string CounterpartyIdentificationType = "04",
    string? VoidPath = null,
    DateOnly? VoidDeadline = null,
    string? VoidMessage = null);
