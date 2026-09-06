namespace Ecunexo.Billing.Core.Emitter;

public sealed record XadesSignatureProfile
{
    public string Standard { get; }
    public string SchemaVersion { get; }
    public string SignatureType { get; }
    public string DigestAlgorithm { get; }
    public string SignatureAlgorithm { get; }
    public int KeyLengthBits { get; }

    private XadesSignatureProfile(
        string standard,
        string schemaVersion,
        string signatureType,
        string digestAlgorithm,
        string signatureAlgorithm,
        int keyLengthBits)
    {
        Standard = standard;
        SchemaVersion = schemaVersion;
        SignatureType = signatureType;
        DigestAlgorithm = digestAlgorithm;
        SignatureAlgorithm = signatureAlgorithm;
        KeyLengthBits = keyLengthBits;
    }

    public static XadesSignatureProfile SriXadesBesDefault() =>
        new(
            "XAdES-BES",
            "1.0.0",
            "ENVELOPED",
            "SHA1",
            "RSA-SHA1",
            2048);
}
