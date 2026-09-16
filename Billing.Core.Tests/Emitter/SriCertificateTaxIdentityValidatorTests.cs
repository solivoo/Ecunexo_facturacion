using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Ecunexo.Billing.Core.Emitter.Services;

namespace Ecunexo.Billing.Core.Tests.Emitter;

public class SriCertificateTaxIdentityValidatorTests
{
    [Fact(DisplayName = "Certificado con RUC exacto de 13 dígitos en Subject es válido")]
    public void IsCertificateValidForRuc_ExactRucInSubject_ReturnsTrue()
    {
        using var cert = CreateSelfSignedCertificate("CN=EMPRESA PRUEBA, SERIALNUMBER=0993397804001");

        var valid = SriCertificateTaxIdentityValidator.IsCertificateValidForRuc(
            cert,
            "0993397804001",
            out var detected,
            out var reason);

        Assert.True(valid);
        Assert.Equal("0993397804001", detected);
        Assert.Null(reason);
    }

    [Fact(DisplayName = "Certificado de persona natural con cédula de 10 dígitos es válido para su RUC terminado en 001")]
    public void IsCertificateValidForRuc_NaturalPersonCedula_ReturnsTrue()
    {
        using var cert = CreateSelfSignedCertificate(
            "C=EC, O=SECURITY DATA S.A. 2, OU=ENTIDAD DE CERTIFICACION, SERIALNUMBER=0953412020-010526200059, CN=GINA STEPHANY ALVARADO ZAMBRANO");

        var valid = SriCertificateTaxIdentityValidator.IsCertificateValidForRuc(
            cert,
            "0953412020001",
            out var detected,
            out var reason);

        Assert.True(valid);
        Assert.Null(reason);
    }

    [Fact(DisplayName = "Certificado con extensión de RUC de empresa (Security Data OID) es válido")]
    public void IsCertificateValidForRuc_CompanyRucExtension_ReturnsTrue()
    {
        var extOid = "1.3.6.1.4.1.37436.2.1.1"; // Security Data RUC Empresa
        var extData = Encoding.ASCII.GetBytes("0993397804001");

        using var cert = CreateSelfSignedCertificateWithExtension(
            "CN=GINA STEPHANY ALVARADO ZAMBRANO, SERIALNUMBER=0953412020",
            extOid,
            extData);

        var valid = SriCertificateTaxIdentityValidator.IsCertificateValidForRuc(
            cert,
            "0993397804001",
            out var detected,
            out var reason);

        Assert.True(valid);
        Assert.Equal("0993397804001", detected);
        Assert.Null(reason);
    }

    [Fact(DisplayName = "Certificado asignado a tenant registrado con mismo RUC es válido")]
    public void IsCertificateValidForRuc_TenantBindingMatch_ReturnsTrue()
    {
        using var cert = CreateSelfSignedCertificate(
            "C=EC, O=SECURITY DATA S.A. 2, SERIALNUMBER=0953412020-010526200059, CN=GINA STEPHANY ALVARADO ZAMBRANO");

        var valid = SriCertificateTaxIdentityValidator.IsCertificateValidForRuc(
            cert,
            "0993397804001",
            registeredTenantTaxId: "0993397804001",
            out var detected,
            out var reason);

        Assert.True(valid);
        Assert.Null(reason);
    }

    [Fact(DisplayName = "Certificado con RUC de representante legal o empresa no es rechazado")]
    public void EnsureCertificateMatchesEmitter_MismatchedRuc_DoesNotThrow()
    {
        // Certificado perteneciente a 1790016919001 intentando firmar comprobante de 0993397804001
        using var cert = CreateSelfSignedCertificate(
            "CN=OTRA EMPRESA S.A., SERIALNUMBER=1790016919001");

        SriCertificateTaxIdentityValidator.EnsureCertificateMatchesEmitter(cert, "0993397804001");
    }

    [Theory(DisplayName = "IsTaxIdCompatible evalúa correctamente compatibilidad fiscal ecuatoriana")]
    [InlineData("0953412020001", "0953412020001", true)]
    [InlineData("0953412020", "0953412020001", true)]
    [InlineData("0953412020001", "0953412020", true)]
    [InlineData("1790016919001", "0993397804001", false)]
    [InlineData("0953412020", "1790016919001", false)]
    [InlineData("", "0993397804001", false)]
    public void IsTaxIdCompatible_EvaluatesProperly(string certId, string emitterRuc, bool expected)
    {
        Assert.Equal(expected, SriCertificateTaxIdentityValidator.IsTaxIdCompatible(certId, emitterRuc));
    }

    private static X509Certificate2 CreateSelfSignedCertificate(string subjectDn)
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(
            new X500DistinguishedName(subjectDn),
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return req.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(365));
    }

    private static X509Certificate2 CreateSelfSignedCertificateWithExtension(
        string subjectDn,
        string extensionOid,
        byte[] extensionRawData)
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(
            new X500DistinguishedName(subjectDn),
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        req.CertificateExtensions.Add(new X509Extension(extensionOid, extensionRawData, false));

        return req.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(365));
    }
}
