namespace Ecunexo.Billing.Core.Tests;

public class RucTests
{
    [Fact(DisplayName = "RUC válido de 13 dígitos se crea correctamente")]
    public void Create_WithValid13Digits_ShouldReturnRuc()
    {
        var ruc = Ruc.Create("1792146739001");

        Assert.Equal("1792146739001", ruc.Value);
    }

    [Fact(DisplayName = "RUC vacío es rechazado")]
    public void Create_WithEmpty_ShowIdThrow()
    {
        Assert.Throws<ArgumentException>(() => Ruc.Create(""));
    }

    [Fact(DisplayName = "RUC con longitud incorrecta es rechazado")]
    public void Create_Create_WithWrongLength_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Ruc.Create("123"));
    }

    [Fact(DisplayName = "RUC con letras es rechazado")]
    public void Create_WithLetters_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Ruc.Create("123456789ABC1"));
    }

    [Fact(DisplayName = "Dos RUC con mismo valor son iguales (record)")]
    public void TwoRucsWithSameValue_ShouldBeEqual()
    {
        var a = Ruc.Create("1234567890001");
        var b = Ruc.Create("1234567890001");

        Assert.Equal(a, b);
    }


}