namespace Ecunexo.Billing.Domain;

public sealed record EstablishmentCode
{
    public string Value { get; }

    private EstablishmentCode(string value) => Value = value;

    public static EstablishmentCode Create(string value)
    {
        if (value.Length != 3 || !value.All(char.IsDigit))
            throw new ArgumentException("Código de establecimiento inválido");

        return new EstablishmentCode(value);
    }

    public override string ToString() => Value;
}