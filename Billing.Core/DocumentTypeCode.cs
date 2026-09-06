namespace Ecunexo.Billing.Core;

public sealed record DocumentTypeCode
{
    public string Value { get; }

    // Constructor
    private DocumentTypeCode(string value) => Value = value;

    public static DocumentTypeCode Create(string value)
    {
        string[] valid = ["01", "03", "04", "05", "06", "07"];

        if(!valid.Contains(value))
            throw new ArgumentException("Código de tipo de documento inválido");

        return new DocumentTypeCode(value);
    }

    public static DocumentTypeCode Factura => new("01");
    public static DocumentTypeCode LiquidacionCompra => new("03");
    public static DocumentTypeCode NotaCredito => new("04");
    public static DocumentTypeCode NotaDebito => new("05");
    public static DocumentTypeCode GuiaRemision => new("06");
    public static DocumentTypeCode Retencion => new("07");

    public override string ToString() => Value;
}