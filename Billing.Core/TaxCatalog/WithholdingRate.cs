namespace Ecunexo.Billing.Core.TaxCatalog;


// Porcentajes de retención (TABLAS 19 y 20 SRI): retención IVA, Renta, ISD.
public class WithholdingRate
{
    public Guid Id { get; private set; }
    public string TaxType { get; private set; } = string.Empty;
    public string RetentionCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Percentage { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }

    private WithholdingRate() { }

    public static WithholdingRate Create(
        string taxType,
        string retentionCode,
        string description,
        decimal percentage,
        DateOnly validFrom,
        DateOnly? validTo = null)
    {
        if (string.IsNullOrWhiteSpace(taxType))
            throw new ArgumentException("Tipo de impuesto a retener es obligatorio no puede ser nulo o vacío");

        if (string.IsNullOrWhiteSpace(retentionCode))
            throw new ArgumentException("El codigo de retencion es obligatorio");

        if (percentage < 0 || percentage > 100)
            throw new ArgumentOutOfRangeException(nameof(percentage), "Porcentaje debe estar entre 0 y 100");

        if (validTo.HasValue && validTo.Value < validFrom)
            throw new ArgumentException("Fecha fin no puede ser anterior a fecha inicio");

        return new WithholdingRate
        {
            Id = Guid.NewGuid(),
            TaxType = taxType,
            RetentionCode = retentionCode,
            Description = description,
            Percentage = percentage,
            ValidFrom = validFrom,
            ValidTo = validTo
        };
    }

    public bool IsActiveOn(DateOnly date) => date >= ValidFrom && (ValidTo is null || date <= ValidTo);

    public void Deactivate(DateOnly endDate)
    {
        if (endDate < ValidFrom)
            throw new InvalidOperationException("No se puede desactivar antes de la fecha de inicio");

        ValidTo = endDate;
    }
}
