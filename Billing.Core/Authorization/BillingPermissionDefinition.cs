namespace Ecunexo.Billing.Core.Authorization;

public sealed record BillingPermissionDefinition(
    string Code,
    string Resource,
    string Action,
    string Description,
    bool IsMvp);
