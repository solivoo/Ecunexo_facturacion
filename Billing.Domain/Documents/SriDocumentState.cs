namespace Ecunexo.Billing.Domain.Documents;

public enum SriDocumentState
{
    Draft,
    Signed,
    PendingReception,
    Received,
    Returned,
    Processing,
    Authorized,
    NotAuthorized
}