namespace Ecunexo.Billing.Core;

/*
    Money: representa un valor monetario en el sistema de facturación.
    Todo monto (subtotales, impuestos, totales, pagos) se modela con Money.

        - El monto es público porque aparece en el XML y en el RIDE.
        - Es inmutable: una vez creado, no se puede cambiar el valor.
        - Siempre se redondea a 2 decimales (precisión requerida por el SRI).
        - No acepta valores negativos (un total o impuesto nunca es negativo).
        - La moneda por defecto es "DOLAR" (Ecuador usa USD).

    Patrón: Constructor público (no Factory Method)
        A diferencia de Ruc, la validación es simple (solo rechazar negativos),
        por eso el constructor es público y no se necesita un método Create.

    Sobrecarga de operadores:
        Se puede sumar y restar Money directamente con + y -.
        Cada operación devuelve un NUEVO Money (no modifica los originales).

    Alcance:
        Posición: Domain
        Clase:
            Acceso: (public) todos lo usan — facturas, líneas, impuestos, pagos
            Modificador: (sealed) no hay subtipos de dinero
            Tipo: (record) igualdad por valor — dos Money(100, "DOLAR") son iguales

        Propiedades:
            Amount: (decimal) el monto, solo lectura, redondeado a 2 decimales
            Currency: (string?) la moneda, solo lectura, default "DOLAR"

        Constructor:
            Acceso: (public) cualquiera puede crear Money con new Money(100)
            Valida: monto no negativo
            Redondea: a 2 decimales con Math.Round

        Propiedad estática:
            Zero: atajo para new Money(0) — útil para inicializar acumuladores

        Operadores:
            +  : suma dos Money y devuelve uno nuevo
            -  : resta dos Money y devuelve uno nuevo
*/

public sealed record Money{
    public decimal Amount { get; }
    public string? Currency { get; }

    public Money(decimal amount, string? currency = "DOLAR"){
        if(amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "El monto no puede ser negativo");

        this.Amount = Math.Round(amount, 2);
        this.Currency = currency;
    }

    public static Money Zero => new(0m);

    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount, a.Currency);
    public static Money operator -(Money a, Money b) => new(a.Amount - b.Amount, a.Currency);
}