namespace Ecunexo.Billing.Api.Contracts.Emitters;

public sealed record CreateEmitterRequest(
    string Ruc,
    string BusinessName,
    string MainAddress,
    string? TradeName);

public sealed record CreateEmitterResponse(Guid EmitterId, string Ruc);
