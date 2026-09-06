namespace Ecunexo.Billing.Core.Emitter;

public sealed record CertificateStorageRef
{
    public string Provider { get; }
    public string KeyId { get; }
    public CertificateLocation Location { get; }

    private CertificateStorageRef(string provider, string keyId, CertificateLocation location)
    {
        Provider = provider;
        KeyId = keyId;
        Location = location;
    }

    public static CertificateStorageRef Create(string provider, string keyId, CertificateLocation location)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("El proveedor de almacenamiento es obligatorio.");

        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("El identificador de clave es obligatorio.");

        return new CertificateStorageRef(provider, keyId, location);
    }
}
