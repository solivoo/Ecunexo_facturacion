namespace Ecunexo.Billing.Core.Emitter;

/// <summary>Datos del emisor necesarios para armar el XML (no viven en la factura).</summary>
public sealed record InvoiceXmlEmitterContext(
    string BusinessName,
    string MainAddress,
    string? TradeName = null,
    string EnvironmentCode = "1",
    string EmissionTypeCode = "1",
    string? SoftwareProviderRuc = null);
