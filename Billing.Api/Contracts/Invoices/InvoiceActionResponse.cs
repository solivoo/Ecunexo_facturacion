namespace Ecunexo.Billing.Api.Contracts.Invoices;

public sealed record InvoiceActionResponse(
    Guid InvoiceId,
    string State,
    string? AccessKey,
    string? SriTransmissionState,
    string Message,
    IReadOnlyList<SriMessageResponse> Messages);
