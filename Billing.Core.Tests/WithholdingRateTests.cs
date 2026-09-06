using Ecunexo.Billing.Core.TaxCatalog;

namespace Ecunexo.Billing.Core.Tests;

public class WithholdingRateTests
{
    [Fact(DisplayName = "Retención IVA 30% se crea con datos válidos")]
    public void Create_WithValidData_ShouldSucceed()
    {
        var rate = WithholdingRate.Create("1", "9", "Retención IVA 30%", 30m, new DateOnly(2024, 1, 1));

        Assert.Equal("1", rate.TaxType);
        Assert.Equal("9", rate.RetentionCode);
        Assert.Equal(30m, rate.Percentage);
        Assert.NotEqual(Guid.Empty, rate.Id);
    }

    [Fact(DisplayName = "Tipo de impuesto vacío es rechazado")]
    public void Create_WithEmptyTaxType_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            WithholdingRate.Create("", "9", "Retención IVA 30%", 30m, new DateOnly(2024, 1, 1)));
    }

    [Fact(DisplayName = "Porcentaje negativo es rechazado")]
    public void Create_WithNegativePercentage_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WithholdingRate.Create("1", "9", "Retención", -5m, new DateOnly(2024, 1, 1)));
    }

    [Fact(DisplayName = "Porcentaje mayor a 100 es rechazado")]
    public void Create_WithOver100Percentage_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WithholdingRate.Create("1", "9", "Retención", 101m, new DateOnly(2024, 1, 1)));
    }

    [Fact(DisplayName = "Retención sin fecha fin está activa en cualquier fecha futura")]
    public void IsActiveOn_WithNoEndDate_ShouldBeActive()
    {
        var rate = WithholdingRate.Create("1", "9", "Retención IVA 30%", 30m, new DateOnly(2024, 1, 1));

        Assert.True(rate.IsActiveOn(new DateOnly(2026, 5, 25)));
    }

    [Fact(DisplayName = "Retención no está activa antes de su fecha de inicio")]
    public void IsActiveOn_BeforeStartDate_ShouldBeFalse()
    {
        var rate = WithholdingRate.Create("1", "9", "Retención IVA 30%", 30m, new DateOnly(2024, 1, 1));

        Assert.False(rate.IsActiveOn(new DateOnly(2023, 12, 31)));
    }

    [Fact(DisplayName = "Desactivar retención le pone fecha fin")]
    public void Deactivate_ShouldSetEndDate()
    {
        var rate = WithholdingRate.Create("1", "9", "Retención IVA 30%", 30m, new DateOnly(2024, 1, 1));

        rate.Deactivate(new DateOnly(2024, 12, 31));

        Assert.False(rate.IsActiveOn(new DateOnly(2025, 1, 1)));
    }
}