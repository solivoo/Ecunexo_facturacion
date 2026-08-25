namespace Ecunexo.Billing.Infrastructure.Secrets.Infisical;

public sealed class InfisicalOptions
{
    public const string SectionName = "Infisical";

    /// <summary>URL base del vault Infisical, sin barra final.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string WorkspaceId { get; set; } = string.Empty;

    public string Environment { get; set; } = "dev";

    public string SecretPath { get; set; } = "/";

    public string CertificateSecretName { get; set; } = "keyfacturacion";

    public string PasswordSecretName { get; set; } = "passfacturacion";

    /// <summary>
    /// Client Id de Universal Auth. Configurar mediante variable de entorno <c>Infisical__ClientId</c>.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client Secret de Universal Auth. Configurar mediante variable de entorno <c>Infisical__ClientSecret</c>.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Anticipación en segundos al renovar el access token en caché.</summary>
    public int TokenCacheSkewSeconds { get; set; } = 120;

    /// <summary>Duración en minutos de la caché del material PKCS#12 en memoria.</summary>
    public int CertificateCacheMinutes { get; set; } = 30;
}
