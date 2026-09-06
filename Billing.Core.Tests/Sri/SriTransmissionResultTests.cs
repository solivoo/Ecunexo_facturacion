using Ecunexo.Billing.Core.Sri;

namespace Ecunexo.Billing.Core.Tests;

public class SriTransmissionResultTests
{
    private static string ValidKey => ClaveAcceso.Create(new ClaveAccesoComponents(
        new DateOnly(2024, 1, 21),
        DocumentTypeCode.Factura,
        Ruc.Create("1792146739001"),
        "1",
        EstablishmentCode.Create("001"),
        EmissionPoint.Create("001"),
        SequentialNumber.Create("000000001"),
        12345678,
        "1")).Value;

    [Fact(DisplayName = "Resultado autorizado exige XML y fecha")]
    public void Create_WhenAuthorized_RequiresXmlAndDate()
    {
        var result = SriTransmissionResult.Create(
            ValidKey,
            SriTransmissionState.Authorized,
            authorizedXml: "<autorizacion/>",
            authorizationDate: DateTimeOffset.UtcNow);

        Assert.Equal(SriTransmissionState.Authorized, result.State);
        Assert.NotNull(result.AuthorizedXml);
    }

    [Fact(DisplayName = "Clave distinta de 49 dígitos rechazada")]
    public void Create_WithInvalidKeyLength_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SriTransmissionResult.Create("123", SriTransmissionState.Received));
    }

    [Fact(DisplayName = "Autorizado sin XML rechazado")]
    public void Create_AuthorizedWithoutXml_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SriTransmissionResult.Create(ValidKey, SriTransmissionState.Authorized));
    }
}
