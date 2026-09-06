namespace Ecunexo.Billing.Core.TaxCatalog.Ports;

public interface IWithholdingRateRepository
{
    WithholdingRate? GetByRetentionCode(string taxType, string retentionCode, DateOnly date);
    IReadOnlyList<WithholdingRate> GetActiveByType(string taxType, DateOnly date);
}