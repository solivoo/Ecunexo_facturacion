using Ecunexo.Billing.Infrastructure.Signing;

namespace Ecunexo.Billing.Infrastructure.Tests;

public class StubElectronicSignatureServiceTests
{
    [Fact(DisplayName = "Firma stub envuelve el XML original")]
    public async Task SignXmlAsync_ReturnsWrappedPayload()
    {
        var service = new StubElectronicSignatureService();
        var xml = System.Text.Encoding.UTF8.GetBytes("<factura/>");

        var signed = await service.SignXmlAsync(Guid.NewGuid(), xml);

        var text = System.Text.Encoding.UTF8.GetString(signed);
        Assert.Contains("<signed", text);
        Assert.Contains(Convert.ToBase64String(xml), text);
    }
}
