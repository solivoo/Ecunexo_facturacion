namespace Ecunexo.Billing.Core.Documents.Services;

/// <summary>
/// Calcula los totales de una factura a partir de sus líneas.
///
/// Ejemplo de líneas:
///   Línea 1: Producto A - Cantidad: 2, Precio: $10, Impuesto IVA 12%
///     LineTotalWithoutTax = $20
///     Taxes = [ { TaxCode: "2", RateCode: "2", TaxableBase: $20, Value: $2.40 } ]
///   Línea 2: Producto B - Cantidad: 1, Precio: $50, Impuesto IVA 0%
///     LineTotalWithoutTax = $50
///     Taxes = [ { TaxCode: "2", RateCode: "0", TaxableBase: $50, Value: $0.00 } ]
/// Resultado esperado:
///   Subtotal = $70
///   TaxTotals = [ { TaxCode: "2", RateCode: "2", TaxableBase: $20, Value: $2.40 }, { TaxCode: "2", RateCode: "0", TaxableBase: $50, Value: $0.00 }]
///   GrandTotal = $72.40
/// </summary>
public static class InvoiceTotalsCalculator
{
    /// <summary>
    /// Calcula el subtotal, los totales de impuestos agrupados y el total general de una lista de líneas de factura.
    /// </summary>
    /// <param name="lines">Lista de líneas de factura</param>
    /// <returns>
    /// Tupla con:
    ///   Subtotal (sin impuestos),
    ///   Listado de totales de impuestos agrupados,
    ///   Total general (subtotal + impuestos).
    /// </returns>
    public static (Money Subtotal, IReadOnlyList<DocumentTaxTotal> TaxTotals, Money GrandTotal) Calculate(IReadOnlyList<InvoiceLine> lines)
    {

        // Validación: Debe haber al menos una línea en la factura
        if (lines.Count == 0)
            throw new ArgumentException("Se requiere al menos una línea");

        // Calcula el subtotal sumando todos los montos sin impuestos de cada línea
        // Ejemplo: Si hay líneas con LineTotalWithoutTax de $20 y $50, el subtotal será $70
        var subtotal = lines.Aggregate(
            Money.Zero,
            (acc, line) => acc + line.LineTotalWithoutTax
        );

        // Agrupa todos los impuestos de todas las líneas por TaxCode y RateCode,
        // y suma la base imponible y el valor del impuesto para cada grupo.
        // Por ejemplo, agrupa los IVAs 12% de todas las líneas, suma sus bases y valores.
        var taxTotals = lines
            .SelectMany(l => l.Taxes)
            .GroupBy(t => (t.TaxCode, t.RateCode)) // Agrupa por tipo y porcentaje de impuesto ejemplo: (2, 2) y (2, 0)
            .Select(g => new DocumentTaxTotal( // Crea un total de impuestos para cada grupo
                g.Key.TaxCode, // Tipo de impuesto ejemplo: 2
                g.Key.RateCode, // Porcentaje de impuesto ejemplo: 2
                g.Aggregate(Money.Zero, (a, t) => a + t.TaxableBase), // Suma bases imponibles del grupo
                g.Aggregate(Money.Zero, (a, t) => a + t.Value)        // Suma valores de impuesto del grupo
            ))
            .ToList();

        // Suma el valor de todos los impuestos calculados en taxTotals
        // Ejemplo: si taxTotals tiene $2.40 y $0.00, el taxSum es $2.40
        var taxSum = taxTotals.Aggregate(Money.Zero, (a, t) => a + t.Value);

        // El total general es la suma del subtotal y la suma de impuestos
        // Ejemplo: $70 + $2.40 = $72.40
        var grandTotal = subtotal + taxSum;

        // Retorna la tupla con los valores calculados
        return (subtotal, taxTotals, grandTotal);
    }
}