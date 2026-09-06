namespace Ecunexo.Billing.Core.TaxRules;

/// <summary>
/// Evalúa anulación en línea SRI vs nota de crédito según la regla vigente.
/// No persiste nada: el plazo vive en <see cref="TaxRule"/> versionada.
/// </summary>
public static class SriOnlineVoidEvaluator
{
    public static SriVoidAdvice Evaluate(
        DateOnly issueDate,
        DateOnly asOf,
        bool isConsumerFinal,
        SriOnlineVoidParameters? parameters = null)
    {
        var rule = parameters ?? SriOnlineVoidParameters.CurrentLaw;
        var day = Math.Clamp(rule.DeadlineDayOfFollowingMonth, 1, 31);
        var nextMonth = issueDate.AddMonths(1);
        var lastDay = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        var deadline = new DateOnly(nextMonth.Year, nextMonth.Month, Math.Min(day, lastDay));

        if (rule.ExtendToNextWeekday)
        {
            while (deadline.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                deadline = deadline.AddDays(1);
            }
        }

        if (isConsumerFinal && rule.ConsumerFinalCannotVoid && rule.ConsumerFinalCannotCreditNote)
        {
            return new SriVoidAdvice(
                SriVoidPath.Forbidden,
                deadline,
                day,
                "Las facturas a consumidor final no se anulan ni con nota de crédito una vez transmitidas al SRI.");
        }

        if (isConsumerFinal && rule.ConsumerFinalCannotVoid && asOf <= deadline)
        {
            return new SriVoidAdvice(
                SriVoidPath.Forbidden,
                deadline,
                day,
                "Las facturas a consumidor final no admiten anulación en línea en el SRI.");
        }

        if (asOf <= deadline)
        {
            return new SriVoidAdvice(
                SriVoidPath.OnlineVoid,
                deadline,
                day,
                $"Anulación en línea SRI vigente hasta el {deadline:dd/MM/yyyy} (día {day} del mes siguiente).");
        }

        return new SriVoidAdvice(
            SriVoidPath.CreditNote,
            deadline,
            day,
            $"El plazo de anulación en línea venció el {deadline:dd/MM/yyyy}. Solo procede nota de crédito.");
    }

    public static bool AllowsCreditNote(SriVoidAdvice advice) =>
        advice.Path is SriVoidPath.CreditNote or SriVoidPath.OnlineVoid;
}
