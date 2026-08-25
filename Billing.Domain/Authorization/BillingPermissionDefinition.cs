namespace Ecunexo.Billing.Domain.Authorization;

public sealed record BillingPermissionDefinition(
    string Code,
    string Resource,
    string Action,
    string Description,
    bool IsMvp);
