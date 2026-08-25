namespace Ecunexo.Billing.Domain.Authorization;

/// <summary>
/// Permisos RBAC del módulo facturación (tenant module code: facturacion).
/// Convención: facturacion:{recurso}:{accion}
/// </summary>
public static class BillingPermissions
{
    public const string ModuleCode = "facturacion";

    public const string Read = "facturacion:read";

    public const string ComprobantesRead = "facturacion:comprobantes:read";

    public const string FacturasRead = "facturacion:facturas:read";
    public const string FacturasReadAll = "facturacion:facturas:read.all";
    public const string FacturasCreate = "facturacion:facturas:create";
    public const string FacturasSign = "facturacion:facturas:sign";
    public const string FacturasTransmit = "facturacion:facturas:transmit";
    public const string FacturasAuthorize = "facturacion:facturas:authorize";
    public const string FacturasStatus = "facturacion:facturas:status";

    public const string NotasCreditoRead = "facturacion:notas-credito:read";
    public const string NotasDebitoRead = "facturacion:notas-debito:read";
    public const string GuiasRemisionRead = "facturacion:guias-remision:read";
    public const string RetencionesRead = "facturacion:retenciones:read";
    public const string LiquidacionCompraRead = "facturacion:liquidacion-compra:read";

    public const string CatalogosRead = "facturacion:catalogos:read";
    public const string CatalogosWrite = "facturacion:catalogos:write";

    public const string EmisorRead = "facturacion:emisor:read";
    public const string EmisorWrite = "facturacion:emisor:write";
    public const string EmisorCertificates = "facturacion:emisor:certificates";

    public const string SriRead = "facturacion:sri:read";
}
