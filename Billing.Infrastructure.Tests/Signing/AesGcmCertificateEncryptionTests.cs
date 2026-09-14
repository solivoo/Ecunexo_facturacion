using System.Security.Cryptography;
using System.Text;
using Ecunexo.Billing.Infrastructure.Signing;
using Microsoft.Extensions.Configuration;

namespace Ecunexo.Billing.Infrastructure.Tests.Signing;

public class AesGcmCertificateEncryptionTests
{
    private static readonly byte[] TestMasterKey = SHA256.HashData(Encoding.UTF8.GetBytes("test-master-key-32bytes-long!!"));

    [Fact(DisplayName = "DecryptCertificate descifra correctamente datos con formato de contraseña empaquetado")]
    public void DecryptCertificate_WithPackedPassword_DecryptsSuccessfully()
    {
        // 1. Datos simulados
        var dummyP12 = Encoding.UTF8.GetBytes("fake-pkcs12-data-bytes");
        var dummyPassword = "MySecretPassword123!";

        // 2. Cifrar P12 con nonce y tag individuales
        var p12Nonce = new byte[12];
        var p12Tag = new byte[16];
        var p12Cipher = new byte[dummyP12.Length];
        RandomNumberGenerator.Fill(p12Nonce);

        using (var aes = new AesGcm(TestMasterKey, 16))
        {
            aes.Encrypt(p12Nonce, dummyP12, p12Cipher, p12Tag);
        }

        // 3. Cifrar Contraseña en formato empaquetado (nonce (12) + tag (16) + ciphertext)
        var pwdBytes = Encoding.UTF8.GetBytes(dummyPassword);
        var pwdNonce = new byte[12];
        var pwdTag = new byte[16];
        var pwdCipher = new byte[pwdBytes.Length];
        RandomNumberGenerator.Fill(pwdNonce);

        using (var aes = new AesGcm(TestMasterKey, 16))
        {
            aes.Encrypt(pwdNonce, pwdBytes, pwdCipher, pwdTag);
        }

        var packedPassword = new byte[12 + 16 + pwdCipher.Length];
        Buffer.BlockCopy(pwdNonce, 0, packedPassword, 0, 12);
        Buffer.BlockCopy(pwdTag, 0, packedPassword, 12, 16);
        Buffer.BlockCopy(pwdCipher, 0, packedPassword, 28, pwdCipher.Length);

        // 4. Descifrar con AesGcmCertificateEncryption
        var (decryptedP12, decryptedPassword) = AesGcmCertificateEncryption.DecryptCertificate(
            p12Cipher,
            packedPassword,
            p12Nonce,
            p12Tag,
            TestMasterKey);

        Assert.Equal("fake-pkcs12-data-bytes", Encoding.UTF8.GetString(decryptedP12));
        Assert.Equal(dummyPassword, decryptedPassword);
    }

    [Fact(DisplayName = "DecryptCertificate arroja excepción clara si la contraseña no puede descifrarse")]
    public void DecryptCertificate_WithCorruptedPassword_ThrowsInvalidOperationException()
    {
        var dummyP12 = Encoding.UTF8.GetBytes("fake-pkcs12-data-bytes");
        var p12Nonce = new byte[12];
        var p12Tag = new byte[16];
        var p12Cipher = new byte[dummyP12.Length];
        RandomNumberGenerator.Fill(p12Nonce);

        using (var aes = new AesGcm(TestMasterKey, 16))
        {
            aes.Encrypt(p12Nonce, dummyP12, p12Cipher, p12Tag);
        }

        var corruptedPassword = new byte[] { 1, 2, 3, 4, 5 };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            AesGcmCertificateEncryption.DecryptCertificate(
                p12Cipher,
                corruptedPassword,
                p12Nonce,
                p12Tag,
                TestMasterKey));

        Assert.Contains("No se pudo descifrar la contraseña", ex.Message);
    }

    [Fact(DisplayName = "ResolveMasterKey resuelve clave hex de 64 caracteres")]
    public void ResolveMasterKey_HexKey_Returns32Bytes()
    {
        var hex = Convert.ToHexString(TestMasterKey);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SigningCertificate:MasterKey"] = hex
            })
            .Build();

        var resolved = AesGcmCertificateEncryption.ResolveMasterKey(config);

        Assert.Equal(32, resolved.Length);
        Assert.Equal(TestMasterKey, resolved);
    }

    [Fact(DisplayName = "ResolveMasterKey fallback por defecto retorna 32 bytes")]
    public void ResolveMasterKey_EmptyConfig_ReturnsDefaultKey32Bytes()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var resolved = AesGcmCertificateEncryption.ResolveMasterKey(config);

        Assert.Equal(32, resolved.Length);
        Assert.Equal(SHA256.HashData(Encoding.UTF8.GetBytes("ecunexo-dev-signing-certificate-master-key-32bytes")), resolved);
    }
}
