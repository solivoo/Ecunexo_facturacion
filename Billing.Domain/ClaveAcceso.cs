using System.Globalization;

namespace Ecunexo.Billing.Domain;


public sealed class ClaveAcceso
{
    public string Value { get; }

    // Constructor
    private ClaveAcceso(string value) => Value = value;

    public static ClaveAcceso Create(ClaveAccesoComponents components){

        var fechaStr = components.FechaEmision.ToString("ddMMyyyy", CultureInfo.InvariantCulture);
        var codigoNum = components.CodigoNumerico.ToString().PadLeft(8, '0');

        var first48 = string.Concat(
            fechaStr,
            components.TipoComprobante.Value,
            components.Ruc.Value,
            components.Ambiente,
            components.Estab.Value,
            components.PtoEmi.Value,
            components.Secuencial.Value,
            codigoNum,
            components.TipoEmision
        );

        if(first48.Length != 48)
            throw new ArgumentException("Clave de acceso mal conformada.");

        var checkDigit = CalculateModulo11(first48);

        return new ClaveAcceso(first48 + checkDigit);
    }

    /// <summary>Rehidrata una clave ya emitida (49 dígitos) sin recalcular el dígito verificador.</summary>
    public static ClaveAcceso FromExisting(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 49 || !value.All(char.IsDigit))
            throw new ArgumentException("La clave de acceso debe tener 49 dígitos.");

        return new ClaveAcceso(value);
    }

    private static int CalculateModulo11(string digits)
    {
        int sum = 0;
        int weight = 2;

        for (int i = digits.Length - 1; i >= 0; i--)
        {
            sum += (digits[i] - '0') * weight;
            weight++;
            if (weight > 7) weight = 2;
        }

        int remainder = sum % 11;
        int result = 11 - remainder;

        return result switch
        {
            11 => 0,
            10 => 1,
            _ => result
        };
    }

    private void EnsureMatches(
        EstablishmentCode estab, 
        EmissionPoint pto, 
        SequentialNumber seq, 
        DocumentTypeCode codDoc)
    {
        if(Value[8..10] != codDoc.Value)
            throw new ArgumentException("Tipo de comprobante no coincide con la clave de acceso.");

        if(Value[24..27] != estab.Value)
            throw new ArgumentException("Establecimiento no coincide con la clave de acceso.");

        if(Value[27..30] != pto.Value)
            throw new ArgumentException("Punto de emisión no coincide con la clave de acceso.");

        if(Value[30..39] != seq.Value)
            throw new ArgumentException("Secuencial no coincide con la clave de acceso.");
    }

    public override string ToString() => Value;
}