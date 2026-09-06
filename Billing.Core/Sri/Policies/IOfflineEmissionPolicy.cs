using Ecunexo.Billing.Core.Documents;

namespace Ecunexo.Billing.Core.Sri.Policies;

public interface IOfflineEmissionPolicy
{
    bool MustWaitBeforeAuthorization(TimeSpan configuredDelay);

    bool CanRetryWithSameKey(SriDocumentState state, string? sriErrorCode);

    bool ShouldPollOnly(string? sriErrorCode);
}
