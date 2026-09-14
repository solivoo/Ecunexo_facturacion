namespace Ecunexo.Billing.Core.Emitter;

public enum CertificateLocation
{
    Hsm,
    KeyVault,
    EncryptedFile,
    UsbToken,
    /// <summary>Base de datos de administración cifrada (AES-256-GCM).</summary>
    Database,
    [Obsolete("Ya no se utiliza Infisical.")]
    Infisical,
}
