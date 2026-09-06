using Ecunexo.Billing.Core.TaxRules;

namespace Ecunexo.Billing.Core.Tests.TaxRules;

public class TaxRuleTests
{
    [Fact(DisplayName = "Deactivate cierra la vigencia sin rebobinar el inicio")]
    public void Deactivate_WithEndOnOrAfterStart_SetsValidTo()
    {
        var rule = TaxRule.Create(
            TaxRuleCodes.SriOnlineVoid,
            "Anulación en línea día 7",
            SriOnlineVoidParameters.CurrentLaw.ToJson(),
            new DateOnly(2025, 8, 1));

        rule.Deactivate(new DateOnly(2026, 12, 31));

        Assert.Equal(new DateOnly(2026, 12, 31), rule.ValidTo);
        Assert.False(rule.IsActiveOn(new DateOnly(2027, 1, 1)));
        Assert.True(rule.IsActiveOn(new DateOnly(2026, 12, 31)));
    }

    [Fact(DisplayName = "Deactivate anterior al inicio es rechazado")]
    public void Deactivate_BeforeStart_Throws()
    {
        var rule = TaxRule.Create(
            TaxRuleCodes.SriOnlineVoid,
            "Anulación en línea día 7",
            SriOnlineVoidParameters.CurrentLaw.ToJson(),
            new DateOnly(2025, 8, 1));

        Assert.Throws<ArgumentException>(() => rule.Deactivate(new DateOnly(2025, 7, 31)));
    }

    [Fact(DisplayName = "ToJson y FromJson conservan el plazo")]
    public void ToJson_Roundtrip_PreservesDeadline()
    {
        var original = SriOnlineVoidParameters.Create(15, false, true, false);
        var restored = SriOnlineVoidParameters.FromJson(original.ToJson());

        Assert.Equal(15, restored.DeadlineDayOfFollowingMonth);
        Assert.False(restored.ExtendToNextWeekday);
        Assert.True(restored.ConsumerFinalCannotVoid);
        Assert.False(restored.ConsumerFinalCannotCreditNote);
    }
}
