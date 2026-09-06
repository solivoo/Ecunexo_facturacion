using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Sri;
using Ecunexo.Billing.Infrastructure.Sri.Adapters;

namespace Ecunexo.Billing.Infrastructure.Tests;

public class InMemorySriGatewayTests
{
    [Fact(DisplayName = "Recepción por defecto devuelve RECIBIDA")]
    public async Task SendReceptionAsync_Default_ReturnsReceived()
    {
        var gateway = new InMemorySriGateway();
        var xml = System.Text.Encoding.UTF8.GetBytes(
            "<factura claveAcceso=\"2101202401179214673900110010010000000011234567812345678901\"/>");

        var result = await gateway.SendReceptionAsync(SriEnvironment.Test, xml);

        Assert.Equal(SriTransmissionState.Received, result.State);
        Assert.Equal(49, result.AccessKey.Length);
    }

    [Fact(DisplayName = "Consulta autorización por defecto devuelve AUTORIZADA (stub feliz)")]
    public async Task QueryAuthorizationAsync_Default_ReturnsAuthorized()
    {
        var gateway = new InMemorySriGateway();
        var key = ClaveAcceso.Create(new ClaveAccesoComponents(
            new DateOnly(2024, 1, 21),
            DocumentTypeCode.Factura,
            Ruc.Create("1792146739001"),
            "1",
            EstablishmentCode.Create("001"),
            EmissionPoint.Create("001"),
            SequentialNumber.Create("000000001"),
            12345678,
            "1"));

        var result = await gateway.QueryAuthorizationAsync(SriEnvironment.Test, key);

        Assert.Equal(SriTransmissionState.Authorized, result.State);
        Assert.Equal(key.Value, result.AccessKey);
        Assert.False(string.IsNullOrWhiteSpace(result.AuthorizedXml));
        Assert.NotNull(result.AuthorizationDate);
    }
}
