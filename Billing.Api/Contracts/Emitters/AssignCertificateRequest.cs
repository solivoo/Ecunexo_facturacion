namespace Ecunexo.Billing.Api.Contracts.Emitters;

public sealed record AssignCertificateRequest(
    string SerialNumber,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    string Provider,
    string SecretName,
    string Location);
