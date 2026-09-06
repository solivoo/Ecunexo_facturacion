namespace Ecunexo.Billing.Core.Sri;

public sealed record SriMessage
{
    public string Identifier { get; }
    public string Text { get; }
    public string? Detail { get; }
    public SriMessageType Type { get; }

    private SriMessage(string identifier, string text, string? detail, SriMessageType type)
    {
        Identifier = identifier;
        Text = text;
        Detail = detail;
        Type = type;
    }

    public static SriMessage Create(
        string identifier,
        string text,
        SriMessageType type,
        string? detail = null)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("El identificador del mensaje SRI es obligatorio.");

        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("El texto del mensaje SRI es obligatorio.");

        return new SriMessage(identifier, text, detail, type);
    }
}
