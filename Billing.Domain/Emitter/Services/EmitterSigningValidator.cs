namespace Ecunexo.Billing.Domain.Emitter.Services;

public static class EmitterSigningValidator
{
    public static void EnsureReadyToSign(Emitter emitter)
    {
        ArgumentNullException.ThrowIfNull(emitter);

        if (!emitter.Active)
            throw new InvalidOperationException("El emisor está inactivo y no puede firmar comprobantes.");

        var certificate = emitter.GetCertificate();
        certificate.EnsureValidFor(emitter.Ruc);
    }
}
