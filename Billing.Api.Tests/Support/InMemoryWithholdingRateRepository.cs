using Ecunexo.Billing.Domain.TaxCatalog;
using Ecunexo.Billing.Domain.TaxCatalog.Ports;

namespace Ecunexo.Billing.Api.Tests.Support;

internal sealed class InMemoryWithholdingRateRepository : IWithholdingRateRepository
{
    private readonly List<WithholdingRate> _rates =
    [
        WithholdingRate.Create("REN", "303", "Honorarios", 10m, new DateOnly(2020, 1, 1))
    ];

    public WithholdingRate? GetByRetentionCode(string taxType, string retentionCode, DateOnly date) =>
        _rates.FirstOrDefault(r => r.TaxType == taxType && r.RetentionCode == retentionCode && r.IsActiveOn(date));

    public IReadOnlyList<WithholdingRate> GetActiveByType(string taxType, DateOnly date) =>
        _rates.Where(r => r.TaxType == taxType && r.IsActiveOn(date)).ToList();
}
