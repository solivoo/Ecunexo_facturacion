using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents;
using Ecunexo.Billing.Core.Documents.Services;

namespace Ecunexo.Billing.Core.Tests.Documents;

public class InvoiceLineTests
{
    [Fact(DisplayName = "Línea válida: 2 × 50 − 0 = 100")]
    public void Create_CorrectFormula_DoesNotThrow()
    {
        var line = ValidLine(lineTotal: 100m, quantity: 2m, unitPrice: 50m);

        InvoiceLineValidator.Validate(line); // o solo new si validas en ctor
    }

    [Fact(DisplayName = "LineTotal distinto a cantidad × precio − descuento falla")]
    public void Create_WrongLineTotal_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ValidLine(lineTotal: 99m, quantity: 2m, unitPrice: 50m));

        Assert.Contains("precioTotalSinImpuesto", ex.Message);
    }

    [Fact(DisplayName = "Cantidad cero es rechazada")]
    public void Create_ZeroQuantity_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ValidLine(lineTotal: 0m, quantity: 0m, unitPrice: 50m));
    }

    [Fact(DisplayName = "Descripción vacía es rechazada")]
    public void Create_EmptyDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new InvoiceLine(1, "", 1m, new Money(10m), Money.Zero, new Money(10m), []));
    }

    [Fact(DisplayName = "Con descuento: 3 × 40 − 20 = 100")]
    public void Create_WithDiscount_MatchesFormula()
    {
        var line = new InvoiceLine(
            1,
            "Producto",
            3m,
            new Money(40m),
            new Money(20m),
            new Money(100m),
            []);

        InvoiceLineValidator.Validate(line);
    }

    [Fact(DisplayName = "LineTax coherente: base = total línea, valor = base × tarifa")]
    public void Create_CoherentLineTax_DoesNotThrow()
    {
        var line = ValidLine(lineTotal: 100m, quantity: 1m, unitPrice: 100m);

        InvoiceLineValidator.Validate(line);
    }

    [Fact(DisplayName = "baseImponible distinta a precioTotalSinImpuesto falla")]
    public void Create_WrongTaxableBase_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new InvoiceLine(
                1,
                "Item",
                1m,
                new Money(100m),
                Money.Zero,
                new Money(100m),
                [new LineTax("2", "4", 15m, new Money(99m), new Money(14.85m))]));

        Assert.Contains("baseImponible", ex.Message);
    }

    [Fact(DisplayName = "valor distinto a base × tarifa falla")]
    public void Create_WrongTaxValue_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new InvoiceLine(
                1,
                "Item",
                1m,
                new Money(100m),
                Money.Zero,
                new Money(100m),
                [new LineTax("2", "4", 15m, new Money(100m), new Money(14.99m))]));

        Assert.Contains("valor del impuesto", ex.Message);
    }

    [Fact(DisplayName = "IVA 0%: valor cero es válido")]
    public void Create_ZeroRateTax_DoesNotThrow()
    {
        var line = new InvoiceLine(
            1,
            "Item",
            1m,
            new Money(50m),
            Money.Zero,
            new Money(50m),
            [new LineTax("2", "0", 0m, new Money(50m), new Money(0m))]);

        InvoiceLineValidator.Validate(line);
    }

    [Fact(DisplayName = "valor redondeado: base 10.01 × 15% = 1.50")]
    public void Create_TaxValueRounding_MatchesExpected()
    {
        var line = new InvoiceLine(
            1,
            "Item",
            1m,
            new Money(10.01m),
            Money.Zero,
            new Money(10.01m),
            [new LineTax("2", "4", 15m, new Money(10.01m), new Money(1.50m))]);

        InvoiceLineValidator.Validate(line);
    }

    [Fact(DisplayName = "Sin impuestos en la línea es válido")]
    public void Create_NoTaxes_DoesNotThrow()
    {
        var line = new InvoiceLine(
            1,
            "Item",
            1m,
            new Money(10m),
            Money.Zero,
            new Money(10m),
            []);

        InvoiceLineValidator.Validate(line);
    }

    [Fact(DisplayName = "Impuestos duplicados por código y tarifa son rechazados")]
    public void Create_DuplicateLineTaxes_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new InvoiceLine(
                1,
                "Item",
                1m,
                new Money(100m),
                Money.Zero,
                new Money(100m),
                [
                    new LineTax("2", "4", 15m, new Money(100m), new Money(15m)),
                    new LineTax("2", "4", 15m, new Money(100m), new Money(15m))
                ]));

        Assert.Contains("impuestos duplicados", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Código principal mayor a 25 caracteres es rechazado")]
    public void Create_MainCodeTooLong_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new InvoiceLine(
                1,
                "Item",
                1m,
                new Money(10m),
                Money.Zero,
                new Money(10m),
                [],
                new string('A', 26)));

        Assert.Contains("código principal", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static InvoiceLine ValidLine(decimal lineTotal, decimal quantity, decimal unitPrice)
    {
        var taxValue = Math.Round(lineTotal * 0.15m, 2, MidpointRounding.AwayFromZero);

        return new InvoiceLine(
            1,
            "Item",
            quantity,
            new Money(unitPrice),
            Money.Zero,
            new Money(lineTotal),
            [new LineTax("2", "4", 15m, new Money(lineTotal), new Money(taxValue))]);
    }
}