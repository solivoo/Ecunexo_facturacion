namespace Ecunexo.Billing.Core.Documents;

/// <summary>
/// Reglas para reenvío manual al SRI: solo la factura más antigua pendiente
/// por establecimiento + punto de emisión (no saltar secuenciales).
/// </summary>
public static class InvoiceSriResendRules
{
    public static bool IsResendableState(SriDocumentState state) =>
        state is SriDocumentState.Signed
            or SriDocumentState.PendingReception
            or SriDocumentState.Received
            or SriDocumentState.Returned
            or SriDocumentState.Processing
            or SriDocumentState.NotAuthorized;

    public static bool IsResendableState(string state) =>
        Enum.TryParse<SriDocumentState>(state, ignoreCase: true, out var parsed)
        && IsResendableState(parsed);

    public static bool IsPendingSequenceState(string state) =>
        !string.Equals(state, nameof(SriDocumentState.Authorized), StringComparison.OrdinalIgnoreCase);

    public static string ResolveOutboxOperation(
        SriDocumentState state,
        IEnumerable<string?>? sriMessageIds = null)
    {
        // 43 = clave ya registrada → consultar autorización, no reenviar recepción.
        if (sriMessageIds?.Any(id => id is "43" or "70") == true)
            return "Authorization";

        return state is SriDocumentState.Received
            or SriDocumentState.Processing
            or SriDocumentState.NotAuthorized
            ? "Authorization"
            : "Reception";
    }
}
