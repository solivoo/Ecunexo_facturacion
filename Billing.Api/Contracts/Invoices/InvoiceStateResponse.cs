namespace Ecunexo.Billing.Api.Contracts.Invoices;

public sealed record InvoiceStateResponse(
    Guid InvoiceId,
    string State,
    string? AccessKey,
    string? SriTransmissionState,
    IReadOnlyList<SriMessageResponse> Messages);

public sealed record SriMessageResponse(string Identifier, string Text, string? Detail, string Type);
