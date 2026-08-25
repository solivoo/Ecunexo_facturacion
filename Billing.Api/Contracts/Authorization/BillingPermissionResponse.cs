namespace Ecunexo.Billing.Api.Contracts.Authorization;

public sealed record BillingPermissionResponse(
    string Code,
    string Resource,
    string Action,
    string Description,
    bool IsMvp,
    string ModuleCode);
