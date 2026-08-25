namespace Ecunexo.Billing.Api.Contracts.Ride;

public sealed record RideProviderResponse(
    string? Ruc,
    string LegalName,
    string FooterLine,
    bool UsesEmitterFallback);

public sealed record UpdateRideProviderRequest(
    string? Ruc,
    string? LegalName,
    string? FooterLine);
