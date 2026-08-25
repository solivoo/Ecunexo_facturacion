namespace Ecunexo.Billing.Infrastructure.Ride;

/// <summary>
/// Identidad del proveedor del sistema de facturación (Res. NAC-DGERCGC26-00000027).
/// Un solo RUC de plataforma; no es el RUC del tenant cliente.
/// </summary>
public sealed class RideProviderOptions
{
    public const string SectionName = "RideProvider";
    public const int CampoAdicionalMaxLength = 300;
    public const string CampoNombreRucProveedor = "RUC Proveedor";

    public string Ruc { get; set; } = string.Empty;
    public string LegalName { get; set; } = "EcuNexo";
    public string FooterLine { get; set; } = "Documento generado por EcuNexo";

    public static string? NormalizeRuc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 13 ? digits : null;
    }

    public static string TruncateCampo(string value)
    {
        if (value.Length <= CampoAdicionalMaxLength)
            return value;

        return value[..CampoAdicionalMaxLength];
    }
}
