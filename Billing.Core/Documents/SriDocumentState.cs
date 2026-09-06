namespace Ecunexo.Billing.Core.Documents;

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