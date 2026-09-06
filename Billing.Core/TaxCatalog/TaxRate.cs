namespace Ecunexo.Billing.Core.TaxCatalog;

public class TaxRate
{
    public Guid Id { get; private set; }
    public string TaxCode { get; private set; } = string.Empty;
    public string RateCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }
    public DateOnly ValidFrom { get; private set; } // Fecha de inicio de vigencia de la tarifa
    public DateOnly? ValidTo { get; private set; } // Fecha de fin de vigencia de la tarifa


    // Constructor vacio para EF Core
    private TaxRate() { }

    // Factory method para crear una nueva tarifa con validacion
    public static TaxRate Create(
        string taxCode,
        string rateCode,
        string description,
        decimal rate,
        DateOnly validFrom, // Una tarifa debe tener una fecha inicial obligatoria.
        DateOnly? validTo = null) //Una tarifa no se sabe cuando expira puede ir null
    {
        if (string.IsNullOrEmpty(taxCode))
            throw new ArgumentException("El código de impuesto no puede ser nulo o vacío");

        if (string.IsNullOrEmpty(rateCode))
            throw new ArgumentException("El código de tarifa no puede ser nulo o vacío");

        if (string.IsNullOrEmpty(description))
            throw new ArgumentException("La descripción no puede ser nula o vacía");

        if (rate < 0)
            throw new ArgumentException("La tarifa no puede ser negativa");

        if (validTo.HasValue && validTo.Value < validFrom)
            throw new ArgumentException("La fecha de validez no puede ser anterior a la fecha de inicio");

        return new TaxRate
        {
            Id = Guid.NewGuid(),
            TaxCode = taxCode,
            RateCode = rateCode,
            Description = description,
            Rate = rate,
            ValidFrom = validFrom,
            ValidTo = validTo
        };
    }

    // Metodo para verificar si la tarifa es activa en una fecha determinada
    public bool IsActiveOn(DateOnly date) => date >= ValidFrom && (ValidTo is null || date <= ValidTo);

    // Metodo para desactivar la tarifa
    public void Deactivate(DateOnly endDate)
    {
        if (endDate  < ValidFrom)
            throw new ArgumentException("La fecha de desactivación no puede ser anterior a la fecha de inicio");

        ValidTo = endDate;
    }
}

