using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Ecunexo.Billing.Infrastructure.Signing;

/// <summary>
/// Descifrado AES-256-GCM para certificados (.p12) y contraseñas almacenadas
/// en la base de datos de administración (tenancy.tenant_signing_certificates).
/// </summary>
public static class AesGcmCertificateEncryption
{
    public const int KeySizeBytes = 32; // 256 bits
    public const int NonceSizeBytes = 12; // 96 bits
    public const int TagSizeBytes = 16; // 128 bits

    public static (byte[] P12Bytes, string Password) DecryptCertificate(
        byte[] encryptedData,
        byte[] encryptedPassword,
        byte[] nonce,
        byte[] tag,
        byte[] masterKey)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        ArgumentNullException.ThrowIfNull(encryptedPassword);
        ArgumentNullException.ThrowIfNull(nonce);
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(masterKey);

        // 1. Descifrar archivo .p12
        var p12Bytes = Decrypt(encryptedData, nonce, tag, masterKey);

        // 2. Descifrar contraseña:
        // Formato empaquetado: nonce (12) + tag (16) + ciphertext
        if (encryptedPassword.Length > NonceSizeBytes + TagSizeBytes)
        {
            try
            {
                var pwdNonce = new byte[NonceSizeBytes];
                var pwdTag = new byte[TagSizeBytes];
                var pwdCiphertext = new byte[encryptedPassword.Length - NonceSizeBytes - TagSizeBytes];

                Buffer.BlockCopy(encryptedPassword, 0, pwdNonce, 0, NonceSizeBytes);
                Buffer.BlockCopy(encryptedPassword, NonceSizeBytes, pwdTag, 0, TagSizeBytes);
                Buffer.BlockCopy(encryptedPassword, NonceSizeBytes + TagSizeBytes, pwdCiphertext, 0, pwdCiphertext.Length);

                var pwdBytes = Decrypt(pwdCiphertext, pwdNonce, pwdTag, masterKey);
                var password = Encoding.UTF8.GetString(pwdBytes);
                return (p12Bytes, password);
            }
            catch (CryptographicException)
            {
                // Si falla el desempaquetado, intentar fallback
            }
        }

        // Fallback para formatos planos si se conocen nonce y tag
        try
        {
            var legacyPwdBytes = Decrypt(encryptedPassword, nonce, tag, masterKey);
            return (p12Bytes, Encoding.UTF8.GetString(legacyPwdBytes));
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                "No se pudo descifrar la contraseña del certificado .p12 almacenado. Por favor vuelva a subir el certificado en Ajustes de Empresa -> Facturación Electrónica.",
                ex);
        }
    }

    public static byte[] Decrypt(byte[] ciphertext, byte[] nonce, byte[] tag, byte[] key)
    {
        if (ciphertext == null || ciphertext.Length == 0)
            throw new ArgumentException("El texto cifrado no puede estar vacío.", nameof(ciphertext));

        if (nonce == null || nonce.Length != NonceSizeBytes)
            throw new ArgumentException($"El nonce debe tener {NonceSizeBytes} bytes.", nameof(nonce));

        if (tag == null || tag.Length != TagSizeBytes)
            throw new ArgumentException($"El tag debe tener {TagSizeBytes} bytes.", nameof(tag));

        if (key == null || key.Length != KeySizeBytes)
            throw new ArgumentException($"La clave debe tener {KeySizeBytes} bytes.", nameof(key));

        var plaintext = new byte[ciphertext.Length];
        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    public static byte[] ResolveMasterKey(IConfiguration configuration)
    {
        var rawKey = configuration["SigningCertificate:MasterKey"]
            ?? configuration["SIGNING_CERTIFICATE_MASTER_KEY"]
            ?? Environment.GetEnvironmentVariable("SIGNING_CERTIFICATE_MASTER_KEY");

        if (!string.IsNullOrWhiteSpace(rawKey))
        {
            if (rawKey.Length == 64 && IsHexString(rawKey))
            {
                return Convert.FromHexString(rawKey);
            }

            try
            {
                var fromBase64 = Convert.FromBase64String(rawKey);
                if (fromBase64.Length == KeySizeBytes)
                {
                    return fromBase64;
                }
            }
            catch
            {
                // Fallback a SHA256
            }

            return SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        }

        return SHA256.HashData(Encoding.UTF8.GetBytes("ecunexo-dev-signing-certificate-master-key-32bytes"));
    }

    private static bool IsHexString(string s)
    {
        foreach (var c in s)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                return false;
        }
        return true;
    }
}
