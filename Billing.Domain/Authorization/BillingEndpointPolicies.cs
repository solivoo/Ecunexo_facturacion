namespace Ecunexo.Billing.Domain.Authorization;

/// <summary>
/// Mapa endpoint HTTP → permiso requerido (referencia para policies ASP.NET).
/// </summary>
public static class BillingEndpointPolicies
{
    public static IReadOnlyList<BillingEndpointPolicy> All { get; } =
    [
        new("GET", "/api/v1/billing/menu", BillingPermissions.Read),
        new("GET", "/api/v1/billing/permissions", BillingPermissions.Read),

        new("GET", "/api/v1/catalogs/tax-rates", BillingPermissions.CatalogosRead),
        new("POST", "/api/v1/catalogs/tax-rates", BillingPermissions.CatalogosWrite),
        new("GET", "/api/v1/catalogs/withholding-rates", BillingPermissions.CatalogosRead),
        new("POST", "/api/v1/catalogs/withholding-rates", BillingPermissions.CatalogosWrite),

        new("POST", "/api/v1/emitters", BillingPermissions.EmisorWrite),
        new("GET", "/api/v1/emitters/{emitterId}", BillingPermissions.EmisorRead),
        new("GET", "/api/v1/emitters/{emitterId}/establishments", BillingPermissions.EmisorRead),
        new("POST", "/api/v1/emitters/{emitterId}/establishments", BillingPermissions.EmisorWrite),
        new("POST", "/api/v1/emitters/{emitterId}/certificates", BillingPermissions.EmisorCertificates),

        new("GET", "/api/v1/emitters/{emitterId}/invoices", BillingPermissions.FacturasRead),
        new("POST", "/api/v1/emitters/{emitterId}/invoices", BillingPermissions.FacturasCreate),
        new("POST", "/api/v1/emitters/{emitterId}/invoices/{invoiceId}/sign", BillingPermissions.FacturasSign),
        new("POST", "/api/v1/emitters/{emitterId}/invoices/{invoiceId}/submit-reception", BillingPermissions.FacturasTransmit),
        new("POST", "/api/v1/emitters/{emitterId}/invoices/{invoiceId}/authorize-poll", BillingPermissions.FacturasAuthorize),
        new("POST", "/api/v1/emitters/{emitterId}/invoices/{invoiceId}/sri/retry", BillingPermissions.FacturasTransmit),
        new("GET", "/api/v1/emitters/{emitterId}/invoices/{invoiceId}/sri-status", BillingPermissions.FacturasStatus)
    ];
}

public sealed record BillingEndpointPolicy(string Method, string RouteTemplate, string PermissionCode);
