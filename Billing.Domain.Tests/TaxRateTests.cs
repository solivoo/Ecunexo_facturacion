using Ecunexo.Billing.Domain.TaxCatalog;
namespace Ecunexo.Billing.Domain.Tests;

public class TaxRateTests
{
    [Fact(DisplayName = "Tarifa IVA 15% se crea con datos válidos")]
    public void Create_WithValidData_ShouldSucceed()
    {
        var rate = TaxRate.Create("2", "4", "IVA 15%", 15m, new DateOnly(2024, 4, 1));

        Assert.Equal("2", rate.TaxCode);
        Assert.Equal("4", rate.RateCode);
        Assert.Equal(15m, rate.Rate);
        Assert.NotEqual(Guid.Empty, rate.Id);
    }

    [Fact(DisplayName = "Código de impuesto vacío es rechazado")]
    public void Create_WithEmptyTaxCode_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            TaxRate.Create("", "4", "IVA 15%", 15m, new DateOnly(2024, 4, 1)));
    }

    [Fact(DisplayName = "Tarifa negativa es rechazada")]
    public void Create_WithNegativeRate_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            TaxRate.Create("2", "4", "IVA 15%", -5m, new DateOnly(2024, 4, 1)));
    }

    [Fact(DisplayName = "Fecha fin anterior a fecha inicio es rechazada")]
    public void Create_WithEndDateBeforeStart_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            TaxRate.Create("2", "4", "IVA 15%", 15m,
                new DateOnly(2024, 4, 1),
                new DateOnly(2023, 1, 1)));
    }

    [Fact(DisplayName = "Tarifa sin fecha fin está activa en cualquier fecha futura")]
    public void IsActiveOn_WithNoEndDate_ShouldBeActiveAfterStart()
    {
        var rate = TaxRate.Create("2", "4", "IVA 15%", 15m, new DateOnly(2024, 4, 1));

        Assert.True(rate.IsActiveOn(new DateOnly(2026, 5, 25)));
    }

    [Fact(DisplayName = "Tarifa no está activa antes de su fecha de inicio")]
    public void IsActiveOn_BeforeStartDate_ShouldBeFalse()
    {
        var rate = TaxRate.Create("2", "4", "IVA 15%", 15m, new DateOnly(2024, 4, 1));

        Assert.False(rate.IsActiveOn(new DateOnly(2024, 3, 31)));
    }

    [Fact(DisplayName = "Tarifa no está activa después de su fecha fin")]
    public void IsActiveOn_AfterEndDate_ShouldBeFalse()
    {
        var rate = TaxRate.Create("2", "4", "IVA 15%", 15m,
            new DateOnly(2024, 4, 1),
            new DateOnly(2024, 12, 31));

        Assert.False(rate.IsActiveOn(new DateOnly(2025, 1, 1)));
    }

    [Fact(DisplayName = "Desactivar una tarifa le pone fecha fin")]
    public void Deactivate_ShouldSetEndDate()
    {
        var rate = TaxRate.Create("2", "4", "IVA 15%", 15m, new DateOnly(2024, 4, 1));

        rate.Deactivate(new DateOnly(2024, 12, 31));

        Assert.False(rate.IsActiveOn(new DateOnly(2025, 1, 1)));
    }

    [Fact(DisplayName = "No se puede desactivar antes de la fecha de inicio")]
    public void Deactivate_BeforeStartDate_ShouldThrow()
    {
        var rate = TaxRate.Create("2", "4", "IVA 15%", 15m, new DateOnly(2024, 4, 1));

        Assert.Throws<ArgumentException>(() =>
            rate.Deactivate(new DateOnly(2023, 1, 1)));
    }
}