using Ecunexo.Billing.Core.Documents;

namespace Ecunexo.Billing.Core.Sri.Policies;

public sealed class OfflineEmissionPolicy : IOfflineEmissionPolicy
{
    private static readonly HashSet<string> NonRetryableCodes =
        new(StringComparer.Ordinal) { "2", "10", "56", "57", "63" };

    public bool MustWaitBeforeAuthorization(TimeSpan configuredDelay) =>
        configuredDelay > TimeSpan.Zero;

    public bool CanRetryWithSameKey(SriDocumentState state, string? sriErrorCode)
    {
        if (ShouldPollOnly(sriErrorCode))
            return false;

        // DEVUELTA = el SRI rechazó ese XML/clave. Reenviar lo mismo no resuelve.
        if (state is SriDocumentState.Returned)
            return false;

        if (sriErrorCode is not null && NonRetryableCodes.Contains(sriErrorCode))
            return false;

        return state is SriDocumentState.NotAuthorized or SriDocumentState.Signed;
    }

    public bool ShouldPollOnly(string? sriErrorCode) =>
        sriErrorCode is "43" or "70";
}
