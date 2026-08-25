namespace Ecunexo.Billing.Domain;

public sealed record SequentialNumber
{
    public string Value { get; }

    private SequentialNumber(string value) => Value = value;

    public static SequentialNumber Create(string value)
    {
        if(value.Length != 9 || !value.All(char.IsDigit))
            throw new ArgumentException("Número secuencial inválido");

        if(value == "000000000")
            throw new ArgumentException("Número secuencial no puede ser 000000000");

        return new SequentialNumber(value);
    }

    public override string ToString() => Value;
}