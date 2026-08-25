using Ecunexo.Billing.Domain.Emitter;
using DomainEmitter = Ecunexo.Billing.Domain.Emitter.Emitter;

namespace Ecunexo.Billing.Domain.Tests;

public class EmitterTests
{
    [Fact(DisplayName = "GetNextSequential incrementa por estab+pto+codDoc")]
    public void GetNextSequential_WithExistingConfig_ReturnsIncrementedSequential()
    {
        var emitter = BuildEmitterWithConfig(lastSequential: 41);

        var sequential = emitter.GetNextSequential(
            EstablishmentCode.Create("001"),
            EmissionPoint.Create("001"),
            DocumentTypeCode.Factura);

        Assert.Equal("000000042", sequential.Value);
    }

    [Fact(DisplayName = "GetNextSequential sin configuración falla")]
    public void GetNextSequential_WithoutConfig_Throws()
    {
        var emitter = DomainEmitter.Create(
            Ruc.Create("1792146739001"),
            "Mi Empresa",
            "Dir Matriz");

        var establishment = Establishment.Create(
            EstablishmentCode.Create("001"),
            "Sucursal 001");
        emitter.AddEstablishment(establishment);

        Assert.Throws<InvalidOperationException>(() =>
            emitter.GetNextSequential(
                EstablishmentCode.Create("001"),
                EmissionPoint.Create("001"),
                DocumentTypeCode.Factura));
    }

    [Fact(DisplayName = "Asignar certificado de otro RUC falla")]
    public void AssignCertificate_WithDifferentRuc_Throws()
    {
        var emitter = DomainEmitter.Create(
            Ruc.Create("1792146739001"),
            "Mi Empresa",
            "Dir Matriz");

        var certificate = SigningCertificate.Create(
            Ruc.Create("0999999999001"),
            "SER-002",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            CertificateStorageRef.Create("KeyVault", "cert-002", CertificateLocation.KeyVault));

        Assert.Throws<InvalidOperationException>(() =>
            emitter.AssignCertificate(certificate));
    }

    [Fact(DisplayName = "GetCertificate sin certificado configurado falla")]
    public void GetCertificate_WithoutCertificate_Throws()
    {
        var emitter = DomainEmitter.Create(
            Ruc.Create("1792146739001"),
            "Mi Empresa",
            "Dir Matriz");

        Assert.Throws<InvalidOperationException>(() => emitter.GetCertificate());
    }

    private static DomainEmitter BuildEmitterWithConfig(long lastSequential)
    {
        var emitter = DomainEmitter.Create(
            Ruc.Create("1792146739001"),
            "Mi Empresa",
            "Dir Matriz");

        var establishment = Establishment.Create(
            EstablishmentCode.Create("001"),
            "Sucursal 001");

        establishment.AddEmissionPointConfig(
            EmissionPointConfig.Create(
                EmissionPoint.Create("001"),
                DocumentTypeCode.Factura,
                lastSequential));

        emitter.AddEstablishment(establishment);
        return emitter;
    }
}
