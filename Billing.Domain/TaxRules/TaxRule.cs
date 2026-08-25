namespace Ecunexo.Billing.Domain.TaxRules;

/// <summary>Regla tributaria versionada (IVA ya vive en TaxRate; esto cubre plazos y políticas SRI).</summary>
public sealed class TaxRule
{
    private TaxRule()
    {
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = "{}";
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }

    public static TaxRule Create(
        string code,
        string description,
        string payloadJson,
        DateOnly validFrom,
        DateOnly? validTo = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        if (validTo is not null && validTo.Value < validFrom)
            throw new ArgumentException("La vigencia final no puede ser anterior al inicio.");

        return new TaxRule
        {
            Id = Guid.CreateVersion7(),
            Code = code.Trim().ToLowerInvariant(),
            Description = description.Trim(),
            PayloadJson = payloadJson.Trim(),
            ValidFrom = validFrom,
            ValidTo = validTo,
        };
    }

    public bool IsActiveOn(DateOnly date) =>
        date >= ValidFrom && (ValidTo is null || date <= ValidTo);

    public void Deactivate(DateOnly endDate)
    {
        if (endDate < ValidFrom)
            throw new ArgumentException("La fecha de desactivación no puede ser anterior a la fecha de inicio.");

        ValidTo = endDate;
    }
}
