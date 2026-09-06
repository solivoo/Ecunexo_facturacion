using Ecunexo.Billing.Core.Authorization;

namespace Ecunexo.Billing.Core.Tests.Authorization;

public sealed class BillingPermissionCatalogTests
{
    [Fact(DisplayName = "Catálogo de permisos no tiene códigos duplicados")]
    public void All_HasUniqueCodes()
    {
        var codes = BillingPermissionCatalog.All.Select(x => x.Code).ToList();
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact(DisplayName = "Constantes BillingPermissions están en el catálogo")]
    public void Constants_AreRegistered()
    {
        Assert.NotNull(BillingPermissionCatalog.Find(BillingPermissions.FacturasCreate));
        Assert.NotNull(BillingPermissionCatalog.Find(BillingPermissions.FacturasReadAll));
        Assert.NotNull(BillingPermissionCatalog.Find(BillingPermissions.CatalogosWrite));
    }

    [Fact(DisplayName = "Endpoint policies referencian permisos válidos")]
    public void EndpointPolicies_ReferenceKnownPermissions()
    {
        foreach (var policy in BillingEndpointPolicies.All)
        {
            Assert.NotNull(BillingPermissionCatalog.Find(policy.PermissionCode));
        }
    }
}
