using Ecunexo.Billing.Domain.Emitter;
using Ecunexo.Billing.Domain.Emitter.Services;
using DomainEmitter = Ecunexo.Billing.Domain.Emitter.Emitter;

namespace Ecunexo.Billing.Domain.Tests;

public class EmitterSigningValidatorTests
{
    [Fact(DisplayName = "Emisor activo con certificado válido puede firmar")]
    public void EnsureReadyToSign_WithValidEmitter_DoesNotThrow()
    {
        var emitter = BuildEmitterWithCertificate();

        EmitterSigningValidator.EnsureReadyToSign(emitter);
    }

    [Fact(DisplayName = "Emisor inactivo no puede firmar")]
    public void EnsureReadyToSign_WhenInactive_Throws()
    {
        var emitter = BuildEmitterWithCertificate();
        emitter.Deactivate();

        Assert.Throws<InvalidOperationException>(() =>
            EmitterSigningValidator.EnsureReadyToSign(emitter));
    }

    [Fact(DisplayName = "Emisor sin certificado no puede firmar")]
    public void EnsureReadyToSign_WithoutCertificate_Throws()
    {
        var emitter = DomainEmitter.Create(
            Ruc.Create("1792146739001"),
            "Empresa",
            "Dir matriz");

        Assert.Throws<InvalidOperationException>(() =>
            EmitterSigningValidator.EnsureReadyToSign(emitter));
    }

    private static DomainEmitter BuildEmitterWithCertificate()
    {
        var ruc = Ruc.Create("1792146739001");
        var emitter = DomainEmitter.Create(ruc, "Empresa", "Dir matriz");
        emitter.AssignCertificate(SigningCertificate.Create(
            ruc,
            "SER-001",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            CertificateStorageRef.Create("KeyVault", "cert-001", CertificateLocation.KeyVault)));
        return emitter;
    }
}
