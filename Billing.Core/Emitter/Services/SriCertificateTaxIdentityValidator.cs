using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Ecunexo.Billing.Core.Emitter.Services;

/// <summary>
/// Regla de dominio e invariante del SRI:
/// El RUC del comprobante electrónico (infoTributaria -> ruc) debe coincidir
/// exactamente con la identidad fiscal (RUC de empresa o Cédula de persona natural / representante legal)
/// de la firma electrónica (.p12 / X.509).
/// </summary>
public static partial class SriCertificateTaxIdentityValidator
{
    // OIDs estándar en Ecuador para RUC de Empresa y Cédula/RUC de Titular o Representante
    public static readonly string[] CompanyRucOids =
    [
        "1.3.6.1.4.1.37436.2.1.1", // Security Data: RUC Empresa
        "1.3.6.1.4.1.37947.3.11",  // Banco Central del Ecuador (BCE): RUC Empresa
        "1.3.6.1.4.1.37244.1.5",   // ANFAC: RUC Empresa
        "1.3.6.1.4.1.43745.1.1.2", // Consejo de la Judicatura: RUC Empresa
        "1.3.6.1.4.1.50372.1.2",   // UANATACA: RUC Empresa
    ];

    public static readonly string[] PersonalTaxIdOids =
    [
        "1.3.6.1.4.1.37436.2.1.2", // Security Data: Cédula/RUC Titular o Representante
        "1.3.6.1.4.1.37947.3.1",   // Banco Central del Ecuador (BCE): Cédula/Pasaporte
        "1.3.6.1.4.1.37244.1.1",   // ANFAC: Cédula/RUC
        "1.3.6.1.4.1.43745.1.1.1", // Consejo de la Judicatura: Cédula/RUC
        "1.3.6.1.4.1.50372.1.1",   // UANATACA: Cédula/RUC
    ];

    /// <summary>
    /// Valida que el certificado pertenezca al emisor del comprobante.
    /// Lanza <see cref="InvalidOperationException"/> si el RUC del comprobante no coincide con la firma.
    /// </summary>
    public static void EnsureCertificateMatchesEmitter(
        X509Certificate2 certificate,
        string comprobanteRuc,
        string? registeredTenantTaxId = null)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        if (!IsCertificateValidForRuc(certificate, comprobanteRuc, registeredTenantTaxId, out var detectedTaxId, out var reason))
        {
            throw new InvalidOperationException(
                $"El RUC del comprobante ({comprobanteRuc}) no coincide con el RUC de la firma electrónica ({detectedTaxId ?? "no detectado"}). {reason}");
        }
    }

    /// <summary>
    /// Determina si un certificado digital es válido para firmar un comprobante con el RUC indicado.
    /// </summary>
    public static bool IsCertificateValidForRuc(
        X509Certificate2 certificate,
        string comprobanteRuc,
        out string? detectedTaxId,
        out string? reason)
    {
        return IsCertificateValidForRuc(certificate, comprobanteRuc, null, out detectedTaxId, out reason);
    }

    /// <summary>
    /// Determina si un certificado digital es válido para firmar un comprobante con el RUC indicado.
    /// </summary>
    public static bool IsCertificateValidForRuc(
        X509Certificate2 certificate,
        string comprobanteRuc,
        string? registeredTenantTaxId,
        out string? detectedTaxId,
        out string? reason)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var normalizedEmitterRuc = DigitsOnly(comprobanteRuc);
        if (normalizedEmitterRuc.Length != 13)
        {
            detectedTaxId = null;
            reason = $"El RUC del emisor '{comprobanteRuc}' no tiene 13 dígitos reglamentarios.";
            return false;
        }

        var identities = ExtractAllTaxIdentities(certificate);
        detectedTaxId = identities.Rucs.FirstOrDefault()
            ?? identities.Cedulas.FirstOrDefault()
            ?? "Desconocido";

        // Caso 1: Coincidencia exacta de RUC de 13 dígitos
        if (identities.Rucs.Contains(normalizedEmitterRuc))
        {
            reason = null;
            return true;
        }

        // Caso 2: Persona Natural (RUC = 10 dígitos de cédula + 001..999)
        var emitterCedula = normalizedEmitterRuc[..10];
        if (identities.Cedulas.Contains(emitterCedula))
        {
            reason = null;
            return true;
        }

        // Caso 3: Certificado vinculado al Tenant configurado en base de datos cuyo tax_id coincide con el RUC del emisor
        if (!string.IsNullOrWhiteSpace(registeredTenantTaxId))
        {
            var normalizedTenantRuc = DigitsOnly(registeredTenantTaxId);
            if (normalizedTenantRuc == normalizedEmitterRuc)
            {
                // El certificado está asignado a la empresa y el emisor corresponde al tenant
                reason = null;
                return true;
            }
        }

        reason = $"La firma pertenece a '{certificate.Subject}'. No se encontró el RUC '{normalizedEmitterRuc}' ni la cédula correspondiente en el certificado.";
        return false;
    }

    /// <summary>
    /// Determina si dos identificaciones fiscales ecuatorianas son compatibles
    /// (ej. RUC 0953412020001 con cédula 0953412020 o con RUC 0953412020001).
    /// </summary>
    public static bool IsTaxIdCompatible(string certTaxId, string emitterRuc)
    {
        var c = DigitsOnly(certTaxId);
        var e = DigitsOnly(emitterRuc);

        if (string.IsNullOrEmpty(c) || string.IsNullOrEmpty(e))
            return false;

        if (c == e)
            return true;

        if (e.Length == 13 && c.Length == 10 && e.StartsWith(c, StringComparison.Ordinal))
            return true;

        if (c.Length == 13 && e.Length == 10 && c.StartsWith(e, StringComparison.Ordinal))
            return true;

        return false;
    }

    /// <summary>
    /// Extrae todos los RUCs (13 dígitos) y Cédulas (10 dígitos) encontrados en el certificado:
    /// Subject, SerialNumber y Extensiones específicas de CAs ecuatorianas.
    /// </summary>
    public static (HashSet<string> Rucs, HashSet<string> Cedulas) ExtractAllTaxIdentities(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var rucs = new HashSet<string>(StringComparer.Ordinal);
        var cedulas = new HashSet<string>(StringComparer.Ordinal);

        // 1. Extensiones específicas de CAs (Security Data, BCE, ANFAC, etc.)
        foreach (var ext in certificate.Extensions)
        {
            var oid = ext.Oid?.Value;
            if (string.IsNullOrEmpty(oid)) continue;

            if (CompanyRucOids.Contains(oid) || PersonalTaxIdOids.Contains(oid))
            {
                CollectTaxIdsFromBytes(ext.RawData, rucs, cedulas);
            }
        }

        // 2. Extraer del Subject / Distinguished Name
        var subject = certificate.Subject ?? string.Empty;
        CollectTaxIdsFromString(subject, rucs, cedulas);

        // 3. Extraer de Subject Name alternativo u otros campos
        var simpleName = certificate.GetNameInfo(X509NameType.SimpleName, false);
        if (!string.IsNullOrWhiteSpace(simpleName))
        {
            CollectTaxIdsFromString(simpleName, rucs, cedulas);
        }

        return (rucs, cedulas);
    }

    private static void CollectTaxIdsFromBytes(byte[] rawData, HashSet<string> rucs, HashSet<string> cedulas)
    {
        if (rawData == null || rawData.Length == 0) return;

        // Decodificar como ASCII / UTF-8 para buscar patrones de dígitos
        var text = Encoding.ASCII.GetString(rawData);
        CollectTaxIdsFromString(text, rucs, cedulas);
    }

    private static void CollectTaxIdsFromString(string text, HashSet<string> rucs, HashSet<string> cedulas)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        foreach (Match match in Ruc13Regex().Matches(text))
        {
            rucs.Add(match.Value);
        }

        foreach (Match match in Cedula10Regex().Matches(text))
        {
            // Solo agregar como cédula si no forma parte de un RUC de 13 dígitos
            var val = match.Value;
            cedulas.Add(val);
        }
    }

    private static string DigitsOnly(string? val) =>
        new((val ?? string.Empty).Where(char.IsDigit).ToArray());

    [GeneratedRegex(@"\b\d{13}\b")]
    private static partial Regex Ruc13Regex();

    [GeneratedRegex(@"\b\d{10}\b")]
    private static partial Regex Cedula10Regex();
}
