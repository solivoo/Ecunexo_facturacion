using Ecunexo.Billing.Core.Emitter;
using DomainEmitter = Ecunexo.Billing.Core.Emitter.Emitter;

namespace Ecunexo.Billing.Core.Tests;

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

    [Fact(DisplayName = "GetNextSequential aísla secuenciales entre Test y Production")]
    public void GetNextSequential_IsolatesTestAndProductionCounters()
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
                lastSequential: 533,
                lastTestSequential: 20));

        emitter.AddEstablishment(establishment);

        var estab = EstablishmentCode.Create("001");
        var pto = EmissionPoint.Create("001");

        // Emisión en Pruebas: debe usar lastTestSequential y avanzar a 21
        var testSeq1 = emitter.GetNextSequential(estab, pto, DocumentTypeCode.Factura, Sri.SriEnvironment.Test);
        Assert.Equal("000000021", testSeq1.Value);

        // Emisión en Producción: debe usar lastSequential (533) y avanzar a 534 sin haber sido afectado por pruebas
        var prodSeq1 = emitter.GetNextSequential(estab, pto, DocumentTypeCode.Factura, Sri.SriEnvironment.Production);
        Assert.Equal("000000534", prodSeq1.Value);

        // Nueva emisión en Pruebas: avanza a 22 sin afectar producción
        var testSeq2 = emitter.GetNextSequential(estab, pto, DocumentTypeCode.Factura, Sri.SriEnvironment.Test);
        Assert.Equal("000000022", testSeq2.Value);

        // Nueva emisión en Producción: avanza a 535
        var prodSeq2 = emitter.GetNextSequential(estab, pto, DocumentTypeCode.Factura, Sri.SriEnvironment.Production);
        Assert.Equal("000000535", prodSeq2.Value);
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
