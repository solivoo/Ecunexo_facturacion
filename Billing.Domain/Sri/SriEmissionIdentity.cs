namespace Ecunexo.Billing.Domain.Sri;

/// <summary>
/// Identidad con la que se arma XML y clave de acceso.
/// En Test puede ser el RUC inscrito en celcer, no el del tenant de ensayo.
/// </summary>
public sealed record SriEmissionIdentity(
    Ruc Ruc,
    string BusinessName,
    string MainAddress,
    string? TradeName,
    EstablishmentCode? Establishment,
    EmissionPoint? EmissionPoint,
    bool IsSubstituted);
