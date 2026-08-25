namespace Ecunexo.Billing.Domain.Tests;

public class MoneyTests
{
    [Fact(DisplayName = "Monto positivo se crea con valor correcto y moneda DOLAR")]
    public void Create_WithValidAmount_ShouldReturnMoney()
    {
        var money = new Money(100.50m);
        Assert.Equal(100.50m, money.Amount);
        Assert.Equal("DOLAR", money.Currency);
    }

    [Fact(DisplayName = "Money.Zero representa monto vacío para inicializar totales")]
    public void Create_WithZero_ShouldSucceed()
    {
        var money = Money.Zero;
        Assert.Equal(0m, money.Amount);
    }

    [Fact(DisplayName = "Montos negativos son rechazados")]
    public void Create_WithNegativeAmount_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m));
    }

    [Fact(DisplayName = "Redondeo automático a 2 decimales")]
    public void Create_ShouldRoundToTwoDecimals()
    {
        var money = new Money(10.999m);              
        Assert.Equal(11.00m, money.Amount);        
    }

    [Fact(DisplayName = "Suma de dos Money acumula los montos")]
    public void Add_TwoMoneys_ShouldSumAmounts()
    {
        var a = new Money(10m);
        var b = new Money(20.50m);

        var result = a + b;

        Assert.Equal(30.50m, result.Amount);
    }

    [Fact(DisplayName = "Resta de dos Money calcula la diferencia")]
    public void Subtract_ShouldWork_WhenResultPositive()
    {
        var a = new Money(50m);
        var b = new Money(20m);    

        var result = a - b;

        Assert.Equal(30m, result.Amount);        
    }

}