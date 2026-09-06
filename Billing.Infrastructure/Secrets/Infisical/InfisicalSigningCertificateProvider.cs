using System.Security.Cryptography.X509Certificates;
using Ecunexo.Billing.Core.Emitter.Ports;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecunexo.Billing.Infrastructure.Secrets.Infisical;

/// <summary>
/// Obtiene PKCS#12 (Base64) + password desde Infisical y materializa
/// <see cref="X509Certificate2"/> solo en memoria (<see cref="X509KeyStorageFlags.EphemeralKeySet"/>).
/// </summary>
public sealed class InfisicalSigningCertificateProvider
    : ISigningCertificateProvider, ISigningPkcs12MaterialProvider
{
    private const string CacheKey = "infisical:signing-pkcs12";

    private readonly InfisicalClient _infisical;
    private readonly IMemoryCache _cache;
    private readonly InfisicalOptions _options;
    private readonly ILogger<InfisicalSigningCertificateProvider> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public InfisicalSigningCertificateProvider(
        InfisicalClient infisical,
        IMemoryCache cache,
        IOptions<InfisicalOptions> options,
        ILogger<InfisicalSigningCertificateProvider> logger)
    {
        _infisical = infisical;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<X509Certificate2> GetAsync(CancellationToken cancellationToken = default)
    {
        var material = await LoadCachedAsync(cancellationToken).ConfigureAwait(false);
        return LoadPkcs12(material.PfxBytes, material.Password);
    }

    public async Task<SigningPkcs12Material> GetPkcs12Async(CancellationToken cancellationToken = default)
    {
        var material = await LoadCachedAsync(cancellationToken).ConfigureAwait(false);
        return new SigningPkcs12Material(material.PfxBytes, material.Password);
    }

    private async Task<CachedPkcs12> LoadCachedAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out CachedPkcs12? cached) && cached is not null)
            return cached;

        await _loadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached is not null)
                return cached;

            var pfxBase64 = await _infisical
                .GetSecretValueAsync(_options.CertificateSecretName, cancellationToken)
                .ConfigureAwait(false);
            var password = await _infisical
                .GetSecretValueAsync(_options.PasswordSecretName, cancellationToken)
                .ConfigureAwait(false);

            var pfxBytes = DecodeBase64(pfxBase64);

            using (var probe = LoadPkcs12(pfxBytes, password))
            {
                _logger.LogInformation(
                    "Certificado de firma cargado desde Infisical (Subject={Subject}, NotAfter={NotAfter:u})",
                    probe.Subject,
                    probe.NotAfter);
            }

            var material = new CachedPkcs12(pfxBytes, password);
            var cacheMinutes = Math.Clamp(_options.CertificateCacheMinutes, 1, 120);

            _cache.Set(
                CacheKey,
                material,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheMinutes),
                });

            return material;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private static byte[] DecodeBase64(string value)
    {
        var cleaned = value
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();

        try
        {
            return Convert.FromBase64String(cleaned);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "El secreto del certificado no es Base64 válido (PKCS#12 esperado).", ex);
        }
    }

    private static X509Certificate2 LoadPkcs12(byte[] pfxBytes, string password)
    {
        try
        {
            return X509CertificateLoader.LoadPkcs12(
                pfxBytes,
                password,
                X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "No se pudo cargar el PKCS#12. Verifique Base64 y password en Infisical.", ex);
        }
    }

    private sealed record CachedPkcs12(byte[] PfxBytes, string Password);
}
