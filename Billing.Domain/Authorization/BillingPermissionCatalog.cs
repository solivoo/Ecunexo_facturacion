namespace Ecunexo.Billing.Domain.Authorization;

public static class BillingPermissionCatalog
{
    public static IReadOnlyList<BillingPermissionDefinition> All { get; } =
    [
        Def(BillingPermissions.Read, "facturacion", "read", "Acceder al módulo de facturación en el menú", true),
        Def(BillingPermissions.ComprobantesRead, "comprobantes", "read", "Ver sección de comprobantes electrónicos", true),

        Def(BillingPermissions.FacturasRead, "facturas", "read", "Consultar las facturas que emitió el usuario", true),
        Def(BillingPermissions.FacturasReadAll, "facturas", "read.all", "Consultar todas las facturas del emisor", true),
        Def(BillingPermissions.FacturasCreate, "facturas", "create", "Crear borrador de factura", true),
        Def(BillingPermissions.FacturasSign, "facturas", "sign", "Firmar XML de factura", true),
        Def(BillingPermissions.FacturasTransmit, "facturas", "transmit", "Enviar recepción al SRI", true),
        Def(BillingPermissions.FacturasAuthorize, "facturas", "authorize", "Consultar autorización SRI (poll)", true),
        Def(BillingPermissions.FacturasStatus, "facturas", "status", "Ver estado consolidado SRI", true),

        Def(BillingPermissions.NotasCreditoRead, "notas-credito", "read", "Notas de crédito (SRI 04)", false),
        Def(BillingPermissions.NotasDebitoRead, "notas-debito", "read", "Notas de débito (SRI 05)", false),
        Def(BillingPermissions.GuiasRemisionRead, "guias-remision", "read", "Guías de remisión (SRI 06)", false),
        Def(BillingPermissions.RetencionesRead, "retenciones", "read", "Comprobantes de retención (SRI 07)", false),
        Def(BillingPermissions.LiquidacionCompraRead, "liquidacion-compra", "read", "Liquidación de compra (SRI 03)", false),

        Def(BillingPermissions.CatalogosRead, "catalogos", "read", "Consultar tarifas y retenciones", true),
        Def(BillingPermissions.CatalogosWrite, "catalogos", "write", "Registrar tarifas y retenciones (uno a uno)", true),

        Def(BillingPermissions.EmisorRead, "emisor", "read", "Consultar datos del emisor", true),
        Def(BillingPermissions.EmisorWrite, "emisor", "write", "Registrar o actualizar emisor", true),
        Def(BillingPermissions.EmisorCertificates, "emisor", "certificates", "Gestionar certificado de firma", true),

        Def(BillingPermissions.SriRead, "sri", "read", "Monitoreo de estados SRI", true)
    ];

    public static BillingPermissionDefinition? Find(string code) =>
        All.FirstOrDefault(x => x.Code == code);

    private static BillingPermissionDefinition Def(
        string code,
        string resource,
        string action,
        string description,
        bool isMvp) =>
        new(code, resource, action, description, isMvp);
}
