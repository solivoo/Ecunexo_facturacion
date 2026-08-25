using System.Security.Cryptography.X509Certificates;

namespace Ecunexo.Billing.Domain.Emitter.Ports;

/// <summary>
/// Carga el certificado de firma electrónica en memoria (sin escribir a disco).
/// </summary>
public interface ISigningCertificateProvider
{
    Task<X509Certificate2> GetAsync(CancellationToken cancellationToken = default);
}
