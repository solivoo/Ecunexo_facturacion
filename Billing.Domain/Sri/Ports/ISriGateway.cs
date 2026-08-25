using Ecunexo.Billing.Domain;

namespace Ecunexo.Billing.Domain.Sri.Ports;

public interface ISriGateway
{
    Task<SriTransmissionResult> SendReceptionAsync(
        SriEnvironment environment,
        byte[] signedXml,
        CancellationToken cancellationToken = default);

    Task<SriTransmissionResult> QueryAuthorizationAsync(
        SriEnvironment environment,
        ClaveAcceso accessKey,
        CancellationToken cancellationToken = default);

    Task<SriTransmissionResult> QueryValidityStatusAsync(
        SriEnvironment environment,
        ClaveAcceso accessKey,
        CancellationToken cancellationToken = default);
}
