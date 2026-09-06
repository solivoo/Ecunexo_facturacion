using Ecunexo.Billing.Core.Sri;

namespace Ecunexo.Billing.Core.Tests;

public class SriMessageTests
{
    [Fact(DisplayName = "Mensaje SRI requiere identificador y texto")]
    public void Create_WithEmptyIdentifier_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SriMessage.Create("", "texto", SriMessageType.Error));
    }
}
