using Ecunexo.Billing.Core.Emitter;

namespace Ecunexo.Billing.Core.Tests;

public class XadesSignatureProfileTests
{
    [Fact(DisplayName = "Perfil SRI XAdES-BES cumple ficha técnica offline")]
    public void SriXadesBesDefault_MatchesTechnicalSpec()
    {
        var profile = XadesSignatureProfile.SriXadesBesDefault();

        Assert.Equal("XAdES-BES", profile.Standard);
        Assert.Equal("1.0.0", profile.SchemaVersion);
        Assert.Equal("ENVELOPED", profile.SignatureType);
        Assert.Equal("SHA1", profile.DigestAlgorithm);
        Assert.Equal("RSA-SHA1", profile.SignatureAlgorithm);
        Assert.Equal(2048, profile.KeyLengthBits);
    }
}
