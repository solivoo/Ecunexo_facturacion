using Ecunexo.Billing.Core.TaxRules;

namespace Ecunexo.Billing.Core.Tests.TaxRules;

public class SriOnlineVoidEvaluatorTests
{
    [Fact(DisplayName = "Dentro del día 7 del mes siguiente permite anulación en línea")]
    public void Evaluate_OnDeadlineDay_OnlineVoid()
    {
        var advice = SriOnlineVoidEvaluator.Evaluate(
            new DateOnly(2026, 7, 15),
            new DateOnly(2026, 8, 7),
            isConsumerFinal: false);

        Assert.Equal(SriVoidPath.OnlineVoid, advice.Path);
        Assert.Equal(new DateOnly(2026, 8, 7), advice.OnlineVoidDeadline);
        Assert.True(SriOnlineVoidEvaluator.AllowsCreditNote(advice));
    }

    [Fact(DisplayName = "Pasado el plazo solo nota de crédito")]
    public void Evaluate_AfterDeadline_CreditNote()
    {
        var advice = SriOnlineVoidEvaluator.Evaluate(
            new DateOnly(2026, 7, 15),
            new DateOnly(2026, 8, 8),
            isConsumerFinal: false);

        Assert.Equal(SriVoidPath.CreditNote, advice.Path);
        Assert.False(advice.Path == SriVoidPath.OnlineVoid);
        Assert.True(SriOnlineVoidEvaluator.AllowsCreditNote(advice));
    }

    [Fact(DisplayName = "Si el límite cae domingo se corre al lunes")]
    public void Evaluate_SundayDeadline_ExtendsToMonday()
    {
        var parameters = new SriOnlineVoidParameters(7, true, true, true);
        // 7 dic 2025 = domingo
        var advice = SriOnlineVoidEvaluator.Evaluate(
            new DateOnly(2025, 11, 20),
            new DateOnly(2025, 12, 8),
            isConsumerFinal: false,
            parameters);

        Assert.Equal(new DateOnly(2025, 12, 8), advice.OnlineVoidDeadline);
        Assert.Equal(SriVoidPath.OnlineVoid, advice.Path);
    }

    [Fact(DisplayName = "Consumidor final queda prohibido")]
    public void Evaluate_ConsumerFinal_Forbidden()
    {
        var advice = SriOnlineVoidEvaluator.Evaluate(
            new DateOnly(2026, 7, 15),
            new DateOnly(2026, 8, 1),
            isConsumerFinal: true);

        Assert.Equal(SriVoidPath.Forbidden, advice.Path);
        Assert.False(SriOnlineVoidEvaluator.AllowsCreditNote(advice));
    }

    [Fact(DisplayName = "Día 15 configurable mueve el plazo")]
    public void Evaluate_CustomDay15_UsesThatDeadline()
    {
        var parameters = new SriOnlineVoidParameters(15, false, true, true);
        var advice = SriOnlineVoidEvaluator.Evaluate(
            new DateOnly(2026, 7, 15),
            new DateOnly(2026, 8, 15),
            isConsumerFinal: false,
            parameters);

        Assert.Equal(SriVoidPath.OnlineVoid, advice.Path);
        Assert.Equal(new DateOnly(2026, 8, 15), advice.OnlineVoidDeadline);

        var after = SriOnlineVoidEvaluator.Evaluate(
            new DateOnly(2026, 7, 15),
            new DateOnly(2026, 8, 16),
            isConsumerFinal: false,
            parameters);
        Assert.Equal(SriVoidPath.CreditNote, after.Path);
    }
}
