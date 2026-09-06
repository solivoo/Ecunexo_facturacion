using Ecunexo.Billing.Core;

namespace Ecunexo.Billing.Core.Emitter;

public class SigningCertificate
{
    public Guid Id { get; private set; }
    public Ruc SubjectRuc { get; private set; } = null!;
    public string SerialNumber { get; private set; } = string.Empty;
    public DateTimeOffset NotBefore { get; private set; }
    public DateTimeOffset NotAfter { get; private set; }
    public CertificateStorageRef StorageRef { get; private set; } = null!;
    public bool IsRevoked { get; private set; }

    private SigningCertificate() { }

    public static SigningCertificate Create(
        Ruc subjectRuc,
        string serialNumber,
        DateTimeOffset notBefore,
        DateTimeOffset notAfter,
        CertificateStorageRef storageRef)
    {
        ArgumentNullException.ThrowIfNull(subjectRuc);
        ArgumentNullException.ThrowIfNull(storageRef);

        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("El serial del certificado es obligatorio.");

        if (notAfter <= notBefore)
            throw new ArgumentException("La fecha de expiración debe ser mayor a la fecha inicial.");

        return new SigningCertificate
        {
            Id = Guid.NewGuid(),
            SubjectRuc = subjectRuc,
            SerialNumber = serialNumber,
            NotBefore = notBefore,
            NotAfter = notAfter,
            StorageRef = storageRef,
            IsRevoked = false
        };
    }

    public bool IsExpiredAt(DateTimeOffset instant) => instant > NotAfter;

    public void EnsureValidFor(Ruc emitterRuc)
    {
        ArgumentNullException.ThrowIfNull(emitterRuc);

        if (IsRevoked)
            throw new InvalidOperationException("El certificado está revocado.");

        if (SubjectRuc.Value != emitterRuc.Value)
            throw new InvalidOperationException("El RUC del certificado no coincide con el emisor.");

        if (DateTimeOffset.UtcNow < NotBefore)
            throw new InvalidOperationException("El certificado aún no entra en vigencia.");

        if (IsExpiredAt(DateTimeOffset.UtcNow))
            throw new InvalidOperationException("El certificado está expirado.");
    }

    public void Revoke()
    {
        IsRevoked = true;
    }
}
