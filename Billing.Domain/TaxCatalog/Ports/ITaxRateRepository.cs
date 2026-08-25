using Ecunexo.Billing.Domain.TaxCatalog;
namespace Ecunexo.Billing.Domain.TaxCatalog.Ports;

public interface ITaxRateRepository
{
    TaxRate? GetByRateCodes(string taxCode, string rateCode, DateOnly date);
    IReadOnlyList<TaxRate> GetActiveByTaxCode(string taxCode, DateOnly date);
}