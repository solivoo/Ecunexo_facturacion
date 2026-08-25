namespace Ecunexo.Billing.Domain.Emitter;

public enum CertificateLocation
{
    Hsm,
    KeyVault,
    EncryptedFile,
    UsbToken,
    /// <summary>Almacén de secretos Infisical (autohospedado o cloud).</summary>
    Infisical,
}
