using Ecunexo.Billing.Domain.Documents;

namespace Ecunexo.Billing.Domain.Tests;

public class InvoiceSriResendRulesTests
{
    [Theory]
    [InlineData(SriDocumentState.Returned, true)]
    [InlineData(SriDocumentState.Signed, true)]
    [InlineData(SriDocumentState.NotAuthorized, true)]
    [InlineData(SriDocumentState.Authorized, false)]
    [InlineData(SriDocumentState.Draft, false)]
    public void IsResendableState_MatchesExpected(SriDocumentState state, bool expected)
    {
        Assert.Equal(expected, InvoiceSriResendRules.IsResendableState(state));
    }

    [Theory]
    [InlineData(SriDocumentState.Returned, "Reception")]
    [InlineData(SriDocumentState.Signed, "Reception")]
    [InlineData(SriDocumentState.Received, "Authorization")]
    [InlineData(SriDocumentState.Processing, "Authorization")]
    [InlineData(SriDocumentState.NotAuthorized, "Authorization")]
    public void ResolveOutboxOperation_MatchesExpected(SriDocumentState state, string expected)
    {
        Assert.Equal(expected, InvoiceSriResendRules.ResolveOutboxOperation(state));
    }

    [Fact]
    public void ResolveOutboxOperation_Code43_ForcesAuthorization()
    {
        Assert.Equal(
            "Authorization",
            InvoiceSriResendRules.ResolveOutboxOperation(SriDocumentState.Returned, ["43"]));
    }
}
