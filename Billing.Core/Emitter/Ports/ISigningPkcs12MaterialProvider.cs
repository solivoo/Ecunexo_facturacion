namespace Ecunexo.Billing.Core.Emitter.Ports;

/// <summary>Material PKCS#12 en memoria (bytes + password) para firmado XAdES.</summary>
public sealed record SigningPkcs12Material(byte[] PfxBytes, string Password)
{
    public string ToBase64() => Convert.ToBase64String(PfxBytes);
}

public interface ISigningPkcs12MaterialProvider
{
    Task<SigningPkcs12Material> GetPkcs12Async(CancellationToken cancellationToken = default);
}
