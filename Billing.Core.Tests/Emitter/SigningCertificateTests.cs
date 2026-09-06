using Ecunexo.Billing.Core.Emitter;

namespace Ecunexo.Billing.Core.Tests;

public class SigningCertificateTests
{
    [Fact(DisplayName = "Certificado válido para mismo RUC no lanza")]
    public void EnsureValidFor_WithSameRuc_DoesNotThrow()
    {
        var ruc = Ruc.Create("1792146739001");
        var certificate = ValidCertificate(ruc);

        certificate.EnsureValidFor(ruc);
    }

    [Fact(DisplayName = "Certificado con RUC distinto es rechazado")]
    public void EnsureValidFor_WithDifferentRuc_Throws()
    {
        var certificate = ValidCertificate(Ruc.Create("1792146739001"));

        Assert.Throws<InvalidOperationException>(() =>
            certificate.EnsureValidFor(Ruc.Create("0999999999001")));
    }

    [Fact(DisplayName = "Certificado revocado es rechazado")]
    public void EnsureValidFor_WhenRevoked_Throws()
    {
        var ruc = Ruc.Create("1792146739001");
        var certificate = ValidCertificate(ruc);
        certificate.Revoke();

        Assert.Throws<InvalidOperationException>(() =>
            certificate.EnsureValidFor(ruc));
    }

    [Fact(DisplayName = "IsExpiredAt devuelve true después de NotAfter")]
    public void IsExpiredAt_AfterNotAfter_ReturnsTrue()
    {
        var ruc = Ruc.Create("1792146739001");
        var certificate = SigningCertificate.Create(
            ruc,
            "SER-001",
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero),
            CertificateStorageRef.Create("KeyVault", "cert-001", CertificateLocation.KeyVault));

        Assert.True(certificate.IsExpiredAt(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    private static SigningCertificate ValidCertificate(Ruc ruc)
    {
        return SigningCertificate.Create(
            ruc,
            "SER-001",
            DateTimeOffset.UtcNow.AddDays(-10),
            DateTimeOffset.UtcNow.AddDays(10),
            CertificateStorageRef.Create("KeyVault", "cert-001", CertificateLocation.KeyVault));
    }
}
