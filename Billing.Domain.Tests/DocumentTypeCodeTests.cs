namespace Ecunexo.Billing.Domain.Tests;

public class DocumentTypeCodeTests
{
    [Theory(DisplayName = "Los 6 códigos válidos del SRI (TABLA 3) se crean sin error")]
    [InlineData("01")]
    [InlineData("03")]
    [InlineData("04")]
    [InlineData("05")]
    [InlineData("06")]
    [InlineData("07")]
    public void Create_WithValidCode_ShouldSucceed(string code)
    {
        var result = DocumentTypeCode.Create(code);
        Assert.Equal(code, result.Value);
    }

    [Theory(DisplayName = "Códigos inválidos del SRI son rechazados")]
    [InlineData("08")]
    [InlineData("02")]
    [InlineData("99")]
    [InlineData("ABC")]
    [InlineData("")]
    public void Create_WithInvalidCode_ShouldThrow(string code)
    {
        Assert.Throws<ArgumentException>(() => DocumentTypeCode.Create(code));
    }

    [Fact(DisplayName = "Atajo estático Factura devuelve 01")]
    public void StaticFactura_ShouldReturn01()
    {
        Assert.Equal("01", DocumentTypeCode.Factura.Value);
    }

    [Fact(DisplayName = "Atajo estático Retencion devuelve 07")]
    public void StaticRetencion_ShouldReturn07()
    {
        Assert.Equal("07", DocumentTypeCode.Retencion.Value);
    }

}