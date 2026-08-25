namespace Ecunexo.Billing.Domain.TaxCatalog;

/// <summary>Forma de pago SRI (tabla 24) — código de dos dígitos.</summary>
public sealed class PaymentForm
{
    public string Code { get; }
    public string Description { get; }

    private PaymentForm(string code, string description)
    {
        Code = code;
        Description = description;
    }

    /// <summary>Catálogo habitual SRI para UI / validación.</summary>
    public static IReadOnlyList<PaymentForm> Catalog { get; } =
    [
        Create("01", "SIN UTILIZACION DEL SISTEMA FINANCIERO"),
        Create("15", "COMPENSACION DE DEUDAS"),
        Create("16", "TARJETA DE DEBITO"),
        Create("17", "DINERO ELECTRONICO"),
        Create("18", "TARJETA PREPAGO"),
        Create("19", "TARJETA DE CREDITO"),
        Create("20", "OTROS CON UTILIZACION DEL SISTEMA FINANCIERO"),
        Create("21", "ENDOSO DE TITULOS"),
    ];

    public static PaymentForm Create(string code, string description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Código de forma de pago es obligatorio.", nameof(code));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción de forma de pago es obligatoria.", nameof(description));

        var normalized = code.Trim();
        if (normalized.Length != 2 || !char.IsDigit(normalized[0]) || !char.IsDigit(normalized[1]))
            throw new ArgumentException("El código de forma de pago debe ser de 2 dígitos.", nameof(code));

        return new PaymentForm(normalized, description.Trim());
    }

    /// <summary>Resuelve por código conocido; si no está en catálogo, acepta cualquier código de 2 dígitos.</summary>
    public static PaymentForm FromCode(string? code)
    {
        var normalized = string.IsNullOrWhiteSpace(code) ? "01" : code.Trim();
        if (normalized.Length != 2 || !char.IsDigit(normalized[0]) || !char.IsDigit(normalized[1]))
            throw new ArgumentException("El código de forma de pago debe ser de 2 dígitos.", nameof(code));

        var known = Catalog.FirstOrDefault(x => x.Code == normalized);
        return known ?? Create(normalized, "FORMA DE PAGO");
    }
}
