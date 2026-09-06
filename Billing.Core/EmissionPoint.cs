namespace Ecunexo.Billing.Core;

public sealed record EmissionPoint
{
    public string Value { get; }

    // Constructor
    private EmissionPoint(string value) => Value = value;

    public static EmissionPoint Create(string value)
    {
        if(value.Length != 3 || !value.All(char.IsDigit))
            throw new ArgumentException("Código de punto de emisión inválido");

        return new EmissionPoint(value);
    }

    public override string ToString() => Value;
}