namespace Ecunexo.Billing.Domain.Documents.Services;
using Ecunexo.Billing.Domain.TaxCatalog.Ports;

public static class InvoiceLineValidator
{
    public static void Validate(InvoiceLine line)
    {
        if (line.LineNumber <= 0)
            throw new ArgumentException("El número de línea debe ser mayor que cero.", nameof(line));

        if (string.IsNullOrWhiteSpace(line.Description))
            throw new ArgumentException("La descripción de la línea es obligatoria.", nameof(line));

        if (line.MainCode is { Length: > 25 })
            throw new ArgumentException("El código principal no puede superar 25 caracteres.", nameof(line));

        if (line.Quantity <= 0)
            throw new ArgumentException("La cantidad debe ser mayor que cero.", nameof(line));

        var expected = Math.Round(
            line.Quantity * line.UnitPrice.Amount - line.Discount.Amount,
            2,
            MidpointRounding.AwayFromZero);

        if (line.LineTotalWithoutTax.Amount != expected)
            throw new ArgumentException(
                $"precioTotalSinImpuesto debe ser cantidad × precio − descuento. " +
                $"Esperado: {expected}, recibido: {line.LineTotalWithoutTax.Amount}.",
                nameof(line));

        ValidateTaxes(line);
    }

    private static void ValidateTaxes(InvoiceLine line)
    {
        var duplicateTax = line.Taxes
            .GroupBy(t => (t.TaxCode, t.RateCode))
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateTax is not null)
            throw new ArgumentException(
                $"No se permiten impuestos duplicados en una línea para codigo {duplicateTax.Key.TaxCode}, " +
                $"codigoPorcentaje {duplicateTax.Key.RateCode}.",
                nameof(line));

        foreach (var tax in line.Taxes)
        {
            if (tax.TaxableBase.Amount != line.LineTotalWithoutTax.Amount)
                throw new ArgumentException(
                    $"baseImponible debe ser igual a precioTotalSinImpuesto. " +
                    $"Esperado: {line.LineTotalWithoutTax.Amount}, recibido: {tax.TaxableBase.Amount}.",
                    nameof(line));

            var expectedValue = Math.Round(
                tax.TaxableBase.Amount * tax.Rate / 100m,
                2,
                MidpointRounding.AwayFromZero);

            if (tax.Value.Amount != expectedValue)
                throw new ArgumentException(
                    $"valor del impuesto debe ser baseImponible × tarifa / 100. " +
                    $"Esperado: {expectedValue}, recibido: {tax.Value.Amount}.",
                    nameof(line));
        }
    }

    public static void ValidateAgainstCatalog(
    IReadOnlyList<InvoiceLine> lines,
    DateOnly issueDate,
    ITaxRateRepository taxRateRepository)
    {
        ArgumentNullException.ThrowIfNull(taxRateRepository);

        foreach (var line in lines)
        {
            foreach (var tax in line.Taxes)
                ValidateTaxAgainstCatalog(tax, issueDate, taxRateRepository);
        }
    }

    private static void ValidateTaxAgainstCatalog(
        LineTax tax,
        DateOnly issueDate,
        ITaxRateRepository taxRateRepository)
    {
        var catalogRate = taxRateRepository.GetByRateCodes(tax.TaxCode, tax.RateCode, issueDate);

        if (catalogRate is null)
            throw new ArgumentException(
                $"No existe tarifa vigente en catálogo para codigo {tax.TaxCode}, codigoPorcentaje {tax.RateCode} " +
                $"en fecha {issueDate:yyyy-MM-dd}.",
                nameof(tax));

        if (tax.Rate != catalogRate.Rate)
            throw new ArgumentException(
                $"tarifa en línea ({tax.Rate}) no coincide con catálogo vigente ({catalogRate.Rate}) " +
                $"para codigo {tax.TaxCode}, codigoPorcentaje {tax.RateCode}.",
                nameof(tax));
    }
}