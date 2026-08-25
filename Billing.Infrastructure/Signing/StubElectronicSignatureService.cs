using Ecunexo.Billing.Domain.Emitter.Ports;

namespace Ecunexo.Billing.Infrastructure.Signing;

public sealed class StubElectronicSignatureService : IElectronicSignatureService
{
    public Task<byte[]> SignXmlAsync(
        Guid emitterId,
        byte[] xml,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(xml);

        if (emitterId == Guid.Empty)
            throw new ArgumentException("El identificador del emisor es obligatorio.");

        var signed = $"<signed emitterId=\"{emitterId}\">{Convert.ToBase64String(xml)}</signed>";
        return Task.FromResult(System.Text.Encoding.UTF8.GetBytes(signed));
    }
}
