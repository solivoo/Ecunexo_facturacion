namespace Ecunexo.Billing.Core.Documents.Services;

using Ecunexo.Billing.Core;

public static class InvoiceTotalsValidator
{
    public static void Validate(ElectronicInvoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        Validate(
            invoice.Lines,
            invoice.SubtotalWithoutTax,
            invoice.TaxTotals,
            invoice.GrandTotal);
    }

    public static void Validate(
        IReadOnlyList<InvoiceLine> lines,
        Money subtotalWithoutTax,
        IReadOnlyList<DocumentTaxTotal> taxTotals,
        Money grandTotal)
    {
        var (expectedSubtotal, expectedTaxTotals, expectedGrandTotal) =
            InvoiceTotalsCalculator.Calculate(lines);

        if (subtotalWithoutTax.Amount != expectedSubtotal.Amount)
            throw new ArgumentException(
                $"totalSinImpuestos debe ser la suma de precioTotalSinImpuesto de las líneas. " +
                $"Esperado: {expectedSubtotal.Amount}, recibido: {subtotalWithoutTax.Amount}.",
                nameof(subtotalWithoutTax));

        ValidateTaxTotals(taxTotals, expectedTaxTotals);

        if (grandTotal.Amount != expectedGrandTotal.Amount)
            throw new ArgumentException(
                $"importeTotal debe ser totalSinImpuestos + suma de impuestos (error SRI 52). " +
                $"Esperado: {expectedGrandTotal.Amount}, recibido: {grandTotal.Amount}.",
                nameof(grandTotal));
    }

    private static void ValidateTaxTotals(
        IReadOnlyList<DocumentTaxTotal> actual,
        IReadOnlyList<DocumentTaxTotal> expected)
    {
        var duplicateTax = actual
            .GroupBy(t => (t.TaxCode, t.RateCode))
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateTax is not null)
            throw new ArgumentException(
                $"No se permiten impuestos duplicados en cabecera para código {duplicateTax.Key.TaxCode}, " +
                $"tarifa {duplicateTax.Key.RateCode}.");

        if (actual.Count != expected.Count)
            throw new ArgumentException(
                $"totalConImpuestos debe tener {expected.Count} grupo(s); recibido: {actual.Count}.");

        var expectedByKey = expected.ToDictionary(t => (t.TaxCode, t.RateCode));

        foreach (var tax in actual)
        {
            if (!expectedByKey.TryGetValue((tax.TaxCode, tax.RateCode), out var expectedTax))
                throw new ArgumentException(
                    $"totalConImpuestos contiene un impuesto no esperado: código {tax.TaxCode}, tarifa {tax.RateCode}.");

            if (tax.TaxableBase.Amount != expectedTax.TaxableBase.Amount)
                throw new ArgumentException(
                    $"baseImponible en cabecera para ({tax.TaxCode}, {tax.RateCode}) debe ser {expectedTax.TaxableBase.Amount}; " +
                    $"recibido: {tax.TaxableBase.Amount}.");

            if (tax.Value.Amount != expectedTax.Value.Amount)
                throw new ArgumentException(
                    $"valor en cabecera para ({tax.TaxCode}, {tax.RateCode}) debe ser {expectedTax.Value.Amount}; " +
                    $"recibido: {tax.Value.Amount}.");
        }
    }
}
