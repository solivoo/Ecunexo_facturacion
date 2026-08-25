using Ecunexo.Billing.Api.Contracts.Authorization;
using Ecunexo.Billing.Api.Contracts.Menu;
using Ecunexo.Billing.Api.Menu;
using Ecunexo.Billing.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecunexo.Billing.Api.Controllers;

[ApiController]
[Route("api/v1/billing")]
public sealed class BillingModuleController(IBillingMenuProvider menuProvider) : ControllerBase
{
    /// <summary>
    /// Menú gluBox del módulo facturación (deprecado — usar Identity GET /api/v1/me/menu).
    /// </summary>
    [Obsolete("Usar Ecunexo Identity: GET /api/v1/me/menu?context=operational")]
    [HttpGet("menu")]
    public ActionResult<MenuConfigResponse> GetMenu([FromQuery] string? permissions)
    {
        if (string.IsNullOrWhiteSpace(permissions))
            return Ok(menuProvider.GetFullMenu());

        var userPermissions = permissions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);

        return Ok(menuProvider.GetMenuForPermissions(userPermissions));
    }

    [HttpGet("permissions")]
    public ActionResult<IReadOnlyList<BillingPermissionResponse>> GetPermissions([FromQuery] bool? mvpOnly)
    {
        var source = mvpOnly == true
            ? BillingPermissionCatalog.All.Where(x => x.IsMvp)
            : BillingPermissionCatalog.All;

        var result = source
            .Select(x => new BillingPermissionResponse(
                x.Code,
                x.Resource,
                x.Action,
                x.Description,
                x.IsMvp,
                BillingPermissions.ModuleCode))
            .ToList();

        return Ok(result);
    }
}
