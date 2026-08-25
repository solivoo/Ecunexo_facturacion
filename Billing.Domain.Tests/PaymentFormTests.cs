using Ecunexo.Billing.Domain.TaxCatalog;

namespace Ecunexo.Billing.Domain.Tests;

public class PaymentFormTests
{
    [Fact(DisplayName = "Forma de pago se crea con código y descripción válidos")]
    public void Create_WithValidData_ShouldSucceed()
    {
        var form = PaymentForm.Create("01", "SIN UTILIZACION DEL SISTEMA FINANCIERO");

        Assert.Equal("01", form.Code);
        Assert.Equal("SIN UTILIZACION DEL SISTEMA FINANCIERO", form.Description);
    }

    [Fact(DisplayName = "Código de forma de pago vacío es rechazado")]
    public void Create_WithEmptyCode_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentForm.Create("", "Efectivo"));
    }

    [Fact(DisplayName = "Descripción vacía es rechazada")]
    public void Create_WithEmptyDescription_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentForm.Create("01", ""));
    }

    [Fact(DisplayName = "FromCode usa catálogo conocido")]
    public void FromCode_Known_ShouldResolveDescription()
    {
        var form = PaymentForm.FromCode("19");
        Assert.Equal("19", form.Code);
        Assert.Contains("CREDITO", form.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "FromCode null cae en 01")]
    public void FromCode_Null_DefaultsTo01()
    {
        Assert.Equal("01", PaymentForm.FromCode(null).Code);
    }
}
