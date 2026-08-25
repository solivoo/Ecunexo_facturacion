namespace Ecunexo.Billing.Api.Contracts.Invoices;

public sealed record CreateCreditNoteRequest(
    string Motivo,
    DateOnly? IssueDate = null);

public sealed record CreateCreditNoteResponse(
    Guid CreditNoteId,
    Guid InvoiceId,
    string State,
    string Sequential,
    decimal GrandTotal,
    string? AccessKey,
    string? SriTransmissionState,
    string Message);
