namespace Ecunexo.Billing.Core;

public sealed record LineTax
{
    public string TaxCode { get; }
    public string RateCode { get; }
    public decimal Rate { get; }
    public Money TaxableBase { get; }
    public Money Value { get; }

    public LineTax(string taxCode, string rateCode, decimal rate, Money taxableBase, Money value)
    {
        TaxCode = taxCode;
        RateCode = rateCode;
        Rate = rate;
        TaxableBase = taxableBase;
        Value = value;
    }
}