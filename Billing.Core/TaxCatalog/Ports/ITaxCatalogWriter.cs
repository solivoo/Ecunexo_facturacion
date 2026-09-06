namespace Ecunexo.Billing.Core.TaxCatalog.Ports;

public interface ITaxCatalogWriter
{
    CatalogWriteResult AddTaxRate(TaxRateSeedItem item);
    CatalogWriteResult AddWithholdingRate(WithholdingRateSeedItem item);
    CatalogWriteResult AddSriOnlineVoidRule(SriOnlineVoidRuleSeedItem item);
}

public sealed record TaxRateSeedItem(
    string TaxCode,
    string RateCode,
    string Description,
    decimal Rate,
    DateOnly ValidFrom,
    DateOnly? ValidTo = null);

public sealed record WithholdingRateSeedItem(
    string TaxType,
    string RetentionCode,
    string Description,
    decimal Percentage,
    DateOnly ValidFrom,
    DateOnly? ValidTo = null);

public sealed record SriOnlineVoidRuleSeedItem(
    string Description,
    DateOnly ValidFrom,
    int DeadlineDayOfFollowingMonth,
    bool ExtendToNextWeekday = true,
    bool ConsumerFinalCannotVoid = true,
    bool ConsumerFinalCannotCreditNote = true);

public sealed record CatalogWriteResult(
    int Created,
    int Skipped,
    IReadOnlyList<string> Errors);
