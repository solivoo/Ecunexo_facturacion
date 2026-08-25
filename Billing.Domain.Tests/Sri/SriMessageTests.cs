using Ecunexo.Billing.Domain.Sri;

namespace Ecunexo.Billing.Domain.Tests;

public class SriMessageTests
{
    [Fact(DisplayName = "Mensaje SRI requiere identificador y texto")]
    public void Create_WithEmptyIdentifier_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SriMessage.Create("", "texto", SriMessageType.Error));
    }
}
