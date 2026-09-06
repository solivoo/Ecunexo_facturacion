using Ecunexo.Billing.Core.Documents;
using Ecunexo.Billing.Core.Sri.Policies;

namespace Ecunexo.Billing.Core.Tests;

public class OfflineEmissionPolicyTests
{
    private readonly OfflineEmissionPolicy _policy = new();

    [Theory(DisplayName = "Debe esperar si el delay configurado es positivo")]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void MustWaitBeforeAuthorization_RespectsDelay(int seconds, bool expected)
    {
        var result = _policy.MustWaitBeforeAuthorization(TimeSpan.FromSeconds(seconds));

        Assert.Equal(expected, result);
    }

    [Fact(DisplayName = "Códigos 43 y 70 solo permiten polling de autorización")]
    public void ShouldPollOnly_WhenCode43Or70_ReturnsTrue()
    {
        Assert.True(_policy.ShouldPollOnly("43"));
        Assert.True(_policy.ShouldPollOnly("70"));
        Assert.False(_policy.ShouldPollOnly("35"));
    }

    [Theory(DisplayName = "Códigos bloqueantes / DEVUELTA no reintentan con misma clave")]
    [InlineData("2")]
    [InlineData("10")]
    [InlineData("35")]
    [InlineData("56")]
    [InlineData("57")]
    [InlineData("63")]
    [InlineData("70")]
    [InlineData("43")]
    public void CanRetryWithSameKey_Returned_AlwaysFalse(string code)
    {
        Assert.False(_policy.CanRetryWithSameKey(SriDocumentState.Returned, code));
    }

    [Fact(DisplayName = "NotAuthorized sin código bloqueante sí permite reintento")]
    public void CanRetryWithSameKey_NotAuthorizedWithoutBlockingCode_ReturnsTrue()
    {
        Assert.True(_policy.CanRetryWithSameKey(SriDocumentState.NotAuthorized, "35"));
    }
}
