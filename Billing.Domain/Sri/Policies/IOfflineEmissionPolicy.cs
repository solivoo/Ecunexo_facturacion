using Ecunexo.Billing.Domain.Documents;

namespace Ecunexo.Billing.Domain.Sri.Policies;

public interface IOfflineEmissionPolicy
{
    bool MustWaitBeforeAuthorization(TimeSpan configuredDelay);

    bool CanRetryWithSameKey(SriDocumentState state, string? sriErrorCode);

    bool ShouldPollOnly(string? sriErrorCode);
}
