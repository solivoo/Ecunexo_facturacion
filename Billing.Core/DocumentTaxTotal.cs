namespace Ecunexo.Billing.Core;

public sealed record DocumentTaxTotal
{
    public string TaxCode { get; }
    public string RateCode { get; }
    public Money TaxableBase { get; }
    public Money Value { get; }

    public DocumentTaxTotal(string taxCode, string rateCode, Money taxableBase, Money value)
    {
        TaxCode = taxCode;
        RateCode = rateCode;
        TaxableBase = taxableBase;
        Value = value;
    }
}