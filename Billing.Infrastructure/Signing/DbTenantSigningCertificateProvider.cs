using System.Data;
using System.Data.Common;
using System.Security.Cryptography.X509Certificates;
using Ecunexo.Billing.Core.Emitter.Ports;
using Ecunexo.Billing.Core.Emitter.Services;
using Ecunexo.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ecunexo.Billing.Infrastructure.Signing;

/// <summary>
/// Proveedor de firma digital que consulta y descifra el archivo .p12
/// directamente desde la tabla <c>tenancy.tenant_signing_certificates</c> de la base de datos de administración.
/// </summary>
public sealed class DbTenantSigningCertificateProvider
    : ISigningPkcs12MaterialProvider, ISigningCertificateProvider
{
    private readonly BillingDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DbTenantSigningCertificateProvider> _logger;

    public DbTenantSigningCertificateProvider(
        BillingDbContext db,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<DbTenantSigningCertificateProvider> logger)
    {
        _db = db;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public async Task<X509Certificate2> GetAsync(CancellationToken cancellationToken = default)
    {
        var material = await GetPkcs12Async(cancellationToken).ConfigureAwait(false);
        return X509CertificateLoader.LoadPkcs12(
            material.PfxBytes,
            material.Password,
            X509KeyStorageFlags.EphemeralKeySet);
    }

    public async Task<SigningPkcs12Material> GetPkcs12Async(CancellationToken cancellationToken = default)
    {
        var firstEmitter = await _db.Emitters
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (firstEmitter is not null)
        {
            return await GetPkcs12ForEmitterAsync(firstEmitter.Id, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            "Se requiere el identificador del emisor para consultar el certificado de firma electrónica en la base de datos.");
    }

    public async Task<SigningPkcs12Material> GetPkcs12ForEmitterAsync(
        Guid emitterId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tenant-signing-material:{emitterId}";
        if (_cache.TryGetValue(cacheKey, out SigningPkcs12Material? cached) && cached is not null)
        {
            return cached;
        }

        var emitter = await _db.Emitters
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == emitterId, cancellationToken)
            .ConfigureAwait(false);

        if (emitter is null)
        {
            throw new InvalidOperationException($"Emisor con Id '{emitterId}' no encontrado.");
        }

        var dbMaterial = await TryLoadFromDatabaseAsync(emitter.TenantId, emitter.Ruc, cancellationToken)
            .ConfigureAwait(false);

        if (dbMaterial is not null)
        {
            // Validar que el certificado cargado sea válido, vigente y pertenezca al RUC del emisor
            using (var cert = X509CertificateLoader.LoadPkcs12(dbMaterial.PfxBytes, dbMaterial.Password, X509KeyStorageFlags.EphemeralKeySet))
            {
                if (DateTime.UtcNow > cert.NotAfter)
                {
                    throw new InvalidOperationException(
                        $"El certificado digital de la empresa '{emitter.BusinessName}' (RUC {emitter.Ruc}) expiró el {cert.NotAfter:u}. Renueve la firma en Ajustes de Empresa.");
                }

                if (DateTime.UtcNow < cert.NotBefore)
                {
                    throw new InvalidOperationException(
                        $"El certificado digital de la empresa '{emitter.BusinessName}' (RUC {emitter.Ruc}) aún no entra en vigencia ({cert.NotBefore:u}).");
                }

                // REGLA CORE: El RUC del comprobante / emisor debe coincidir exactamente con la firma electrónica
                SriCertificateTaxIdentityValidator.EnsureCertificateMatchesEmitter(
                    cert,
                    emitter.Ruc,
                    emitter.Ruc);

                _logger.LogInformation(
                    "Certificado de firma cargado y validado OK desde BD tenancy para emisor {Ruc} (Subject={Subject}, NotAfter={NotAfter:u})",
                    emitter.Ruc,
                    cert.Subject,
                    cert.NotAfter);
            }

            _cache.Set(cacheKey, dbMaterial, TimeSpan.FromMinutes(15));
            return dbMaterial;
        }

        throw new InvalidOperationException(
            $"No se encontró una firma digital activa para la empresa '{emitter.BusinessName}' (RUC {emitter.Ruc}) en la base de datos de administración. Por favor cargue su certificado .p12 en Ajustes de Empresa -> Facturación Electrónica.");
    }

    private async Task<SigningPkcs12Material?> TryLoadFromDatabaseAsync(
        Guid? tenantId,
        string ruc,
        CancellationToken cancellationToken)
    {
        try
        {
            var adminDbConnStr = _configuration.GetConnectionString("AdminDb")
                ?? _configuration.GetConnectionString("Tenancy")
                ?? _configuration["ConnectionStrings__AdminDb"]
                ?? _configuration["ADMIN_DB_CONNECTION_STRING"]
                ?? _configuration["TENANCY_DB_CONNECTION_STRING"]
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__AdminDb")
                ?? Environment.GetEnvironmentVariable("ADMIN_DB_CONNECTION_STRING")
                ?? Environment.GetEnvironmentVariable("TENANCY_DB_CONNECTION_STRING");

            DbConnection conn;
            bool shouldDisposeConn = false;
            bool wasClosed = false;

            if (!string.IsNullOrWhiteSpace(adminDbConnStr))
            {
                conn = new Npgsql.NpgsqlConnection(adminDbConnStr);
                shouldDisposeConn = true;
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (!_db.Database.IsRelational())
                    return null;

                conn = _db.Database.GetDbConnection();
                wasClosed = conn.State == ConnectionState.Closed;
                if (wasClosed)
                {
                    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            try
            {
                var cedula = ruc.Length >= 10 ? ruc[..10] : ruc;
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    SELECT encrypted_data, encrypted_password, nonce, tag, subject, subject_tax_id
                    FROM tenancy.tenant_signing_certificates
                    WHERE (
                        (tenant_id IS NOT NULL AND tenant_id = @tenantId)
                        OR subject_tax_id = @ruc
                        OR subject_tax_id = @cedula
                        OR (@ruc LIKE subject_tax_id || '%')
                    )
                      AND is_active = true
                    ORDER BY 
                        CASE WHEN tenant_id = @tenantId THEN 0 ELSE 1 END,
                        created_at DESC
                    LIMIT 1
                """;

                var pTenant = cmd.CreateParameter();
                pTenant.ParameterName = "tenantId";
                pTenant.Value = (object?)tenantId ?? DBNull.Value;
                cmd.Parameters.Add(pTenant);

                var pRuc = cmd.CreateParameter();
                pRuc.ParameterName = "ruc";
                pRuc.Value = ruc;
                cmd.Parameters.Add(pRuc);

                var pCedula = cmd.CreateParameter();
                pCedula.ParameterName = "cedula";
                pCedula.Value = cedula;
                cmd.Parameters.Add(pCedula);

                byte[]? encryptedData = null;
                byte[]? encryptedPassword = null;
                byte[]? nonce = null;
                byte[]? tag = null;

                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    encryptedData = (byte[])reader["encrypted_data"];
                    encryptedPassword = (byte[])reader["encrypted_password"];
                    nonce = (byte[])reader["nonce"];
                    tag = (byte[])reader["tag"];
                }

                if (encryptedData is { Length: > 0 }
                    && encryptedPassword is { Length: > 0 }
                    && nonce is { Length: > 0 }
                    && tag is { Length: > 0 })
                {
                    var masterKey = AesGcmCertificateEncryption.ResolveMasterKey(_configuration);
                    var (p12Bytes, password) = AesGcmCertificateEncryption.DecryptCertificate(
                        encryptedData,
                        encryptedPassword,
                        nonce,
                        tag,
                        masterKey);

                    return new SigningPkcs12Material(p12Bytes, password);
                }
            }
            finally
            {
                if (shouldDisposeConn)
                {
                    await conn.DisposeAsync().ConfigureAwait(false);
                }
                else if (wasClosed && conn.State == ConnectionState.Open)
                {
                    await conn.CloseAsync().ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error al consultar o descifrar certificado desde tenancy.tenant_signing_certificates para RUC {Ruc}",
                ruc);
        }

        return null;
    }
}
