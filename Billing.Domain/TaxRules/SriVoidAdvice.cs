namespace Ecunexo.Billing.Domain.TaxRules;

public static class TaxRuleCodes
{
    /// <summary>Anulación en línea SRI vs nota de crédito (día límite del mes siguiente).</summary>
    public const string SriOnlineVoid = "sri.void.online";

    public const string ConsumidorFinalIdType = "07";
}

public enum SriVoidPath
{
    OnlineVoid,
    CreditNote,
    Forbidden,
}

public sealed record SriOnlineVoidParameters(
    int DeadlineDayOfFollowingMonth,
    bool ExtendToNextWeekday,
    bool ConsumerFinalCannotVoid,
    bool ConsumerFinalCannotCreditNote)
{
    public static SriOnlineVoidParameters CurrentLaw { get; } = new(7, true, true, true);

    public static SriOnlineVoidParameters Create(
        int deadlineDayOfFollowingMonth,
        bool extendToNextWeekday,
        bool consumerFinalCannotVoid,
        bool consumerFinalCannotCreditNote)
    {
        if (deadlineDayOfFollowingMonth is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deadlineDayOfFollowingMonth),
                "El día límite debe estar entre 1 y 31.");
        }

        return new(
            deadlineDayOfFollowingMonth,
            extendToNextWeekday,
            consumerFinalCannotVoid,
            consumerFinalCannotCreditNote);
    }

    public static SriOnlineVoidParameters FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return CurrentLaw;

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        var day = root.TryGetProperty("deadlineDayOfFollowingMonth", out var dayEl)
            ? dayEl.GetInt32()
            : CurrentLaw.DeadlineDayOfFollowingMonth;
        var extend = !root.TryGetProperty("extendToNextWeekday", out var extEl) || extEl.GetBoolean();
        var blockVoid = !root.TryGetProperty("consumerFinalCannotVoid", out var cvEl) || cvEl.GetBoolean();
        var blockNc = !root.TryGetProperty("consumerFinalCannotCreditNote", out var cnEl) || cnEl.GetBoolean();
        return Create(day, extend, blockVoid, blockNc);
    }

    public string ToJson() =>
        System.Text.Json.JsonSerializer.Serialize(
            new SriOnlineVoidPayload(
                DeadlineDayOfFollowingMonth,
                ExtendToNextWeekday,
                ConsumerFinalCannotVoid,
                ConsumerFinalCannotCreditNote),
            JsonOptions);

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    };

    private sealed record SriOnlineVoidPayload(
        int DeadlineDayOfFollowingMonth,
        bool ExtendToNextWeekday,
        bool ConsumerFinalCannotVoid,
        bool ConsumerFinalCannotCreditNote);
}

public sealed record SriVoidAdvice(
    SriVoidPath Path,
    DateOnly OnlineVoidDeadline,
    int DeadlineDayOfFollowingMonth,
    string Message);
