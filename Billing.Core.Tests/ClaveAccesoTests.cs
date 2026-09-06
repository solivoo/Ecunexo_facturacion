namespace Ecunexo.Billing.Core.Tests;

public class ClaveAccesoTests
{
    private ClaveAccesoComponents BuildComponents(
        string fecha = "2024-01-21",
        string codDoc = "01",
        string ruc = "1792146739001",
        string ambiente = "1",
        string estab = "001",
        string ptoEmi = "001",
        string secuencial = "000000001",
        int codigoNumerico = 12345678,
        string tipoEmision = "1")
    {
        return new ClaveAccesoComponents(
            DateOnly.Parse(fecha),
            DocumentTypeCode.Create(codDoc),
            Ruc.Create(ruc),
            ambiente,
            EstablishmentCode.Create(estab),
            EmissionPoint.Create(ptoEmi),
            SequentialNumber.Create(secuencial),
            codigoNumerico,
            tipoEmision
        );
    }




    [Fact(DisplayName = "La clave de acceso tiene 49 dígitos numéricos (TABLA 1)")]
    public void Create_ShouldReturn49Digits()
    {
        var clave = ClaveAcceso.Create(BuildComponents());

        Assert.Equal(49, clave.Value.Length);
        Assert.True(clave.Value.All(char.IsDigit));
    }

    [Fact(DisplayName = "Posiciones 1-8: fecha en formato ddMMyyyy")]
    public void Create_ShouldStartWithDateDdMmYyyy()
    {
        var clave = ClaveAcceso.Create(BuildComponents(fecha: "2024-01-21"));

        Assert.StartsWith("21012024", clave.Value);
    }

    [Fact(DisplayName = "Posiciones 9-10: código del tipo de comprobante")]
    public void Create_ShouldContainDocTypeAtPosition8()
    {
        var clave = ClaveAcceso.Create(BuildComponents(codDoc: "04"));

        Assert.Equal("04", clave.Value[8..10]);
    }

    [Fact(DisplayName = "Posiciones 11-23: RUC del emisor (13 dígitos)")]
    public void Create_ShouldContainRucAtPosition10()
    {
        var clave = ClaveAcceso.Create(BuildComponents(ruc: "1792146739001"));

        Assert.Equal("1792146739001", clave.Value[10..23]);
    }

    [Fact(DisplayName = "Mismos datos de entrada producen la misma clave (determinismo)")]
    public void Create_SameInputs_ShouldProduceSameKey()
    {
        var a = ClaveAcceso.Create(BuildComponents());
        var b = ClaveAcceso.Create(BuildComponents());

        Assert.Equal(a.Value, b.Value);
    }

    [Fact(DisplayName = "Secuenciales distintos generan claves distintas")]
    public void Create_DifferentSequential_ShouldProduceDifferentKey()
    {
        var a = ClaveAcceso.Create(BuildComponents(secuencial: "000000001"));
        var b = ClaveAcceso.Create(BuildComponents(secuencial: "000000002"));

        Assert.NotEqual(a.Value, b.Value);
    }
}