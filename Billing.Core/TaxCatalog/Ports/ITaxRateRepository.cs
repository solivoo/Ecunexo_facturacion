using Ecunexo.Billing.Core.TaxCatalog;
namespace Ecunexo.Billing.Core.TaxCatalog.Ports;

public interface ITaxRateRepository
{
    TaxRate? GetByRateCodes(string taxCode, string rateCode, DateOnly date);
    IReadOnlyList<TaxRate> GetActiveByTaxCode(string taxCode, DateOnly date);
}