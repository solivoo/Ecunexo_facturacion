namespace Ecunexo.Billing.Domain.Sri;

public sealed record SriTransmissionResult
{
    public string AccessKey { get; }
    public SriTransmissionState State { get; }
    public IReadOnlyList<SriMessage> Messages { get; }
    public string? AuthorizedXml { get; }
    public DateTimeOffset? AuthorizationDate { get; }

    private SriTransmissionResult(
        string accessKey,
        SriTransmissionState state,
        IReadOnlyList<SriMessage> messages,
        string? authorizedXml,
        DateTimeOffset? authorizationDate)
    {
        AccessKey = accessKey;
        State = state;
        Messages = messages;
        AuthorizedXml = authorizedXml;
        AuthorizationDate = authorizationDate;
    }

    public static SriTransmissionResult Create(
        string accessKey,
        SriTransmissionState state,
        IReadOnlyList<SriMessage>? messages = null,
        string? authorizedXml = null,
        DateTimeOffset? authorizationDate = null)
    {
        if (string.IsNullOrWhiteSpace(accessKey))
            throw new ArgumentException("La clave de acceso es obligatoria.");

        if (accessKey.Length != 49)
            throw new ArgumentException("La clave de acceso debe tener 49 dígitos.");

        if (state is SriTransmissionState.Authorized)
        {
            if (string.IsNullOrWhiteSpace(authorizedXml))
                throw new ArgumentException("El XML autorizado es obligatorio cuando el estado es Autorizado.");

            if (authorizationDate is null)
                throw new ArgumentException("La fecha de autorización es obligatoria cuando el estado es Autorizado.");
        }

        return new SriTransmissionResult(
            accessKey,
            state,
            messages ?? [],
            authorizedXml,
            authorizationDate);
    }
}
