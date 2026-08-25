namespace Ecunexo.Billing.Domain.Emitter.Ports;

public interface IElectronicSignatureService
{
    Task<byte[]> SignXmlAsync(Guid emitterId, byte[] xml, CancellationToken cancellationToken = default);
}
