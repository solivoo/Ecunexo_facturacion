using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecunexo.Billing.Infrastructure.Secrets.Infisical;

/// <summary>
/// Autenticación Universal Auth + lectura de secretos raw (API v3).
/// Caché en memoria del access token para no saturar Infisical.
/// </summary>
public sealed class InfisicalClient
{
    private readonly HttpClient _http;
    private readonly InfisicalOptions _options;
    private readonly ILogger<InfisicalClient> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    public InfisicalClient(
        HttpClient http,
        IOptions<InfisicalOptions> options,
        ILogger<InfisicalClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetSecretValueAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
            throw new ArgumentException("El nombre del secreto es obligatorio.", nameof(secretName));

        ValidateConfigured();

        var token = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        // API oficial: GET /api/v3/secrets/raw/{secretName}
        // También compatible con ?secretName= (Postman / listados).
        var path = $"api/v3/secrets/raw/{Uri.EscapeDataString(secretName)}"
            + $"?environment={Uri.EscapeDataString(_options.Environment)}"
            + $"&workspaceId={Uri.EscapeDataString(_options.WorkspaceId)}"
            + $"&secretPath={Uri.EscapeDataString(_options.SecretPath)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(
                "Infisical GET secreto {SecretName} falló: {StatusCode} {Body}",
                secretName,
                (int)response.StatusCode,
                Truncate(body, 200));
            throw new InvalidOperationException(
                $"No se pudo leer el secreto '{secretName}' desde Infisical ({(int)response.StatusCode}).");
        }

        var payload = await response.Content.ReadFromJsonAsync<InfisicalSecretResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var value = ResolveSecretValue(payload, secretName);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"El secreto '{secretName}' no existe o está vacío en Infisical.");

        return value;
    }

    private static string? ResolveSecretValue(InfisicalSecretResponse? payload, string secretName)
    {
        if (payload?.Secret is not null
            && (string.IsNullOrEmpty(payload.Secret.SecretKey)
                || string.Equals(payload.Secret.SecretKey, secretName, StringComparison.OrdinalIgnoreCase)))
            return payload.Secret.SecretValue;

        return payload?.Secrets?
            .FirstOrDefault(s => string.Equals(s.SecretKey, secretName, StringComparison.OrdinalIgnoreCase))
            ?.SecretValue;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiresAt)
            return _accessToken;

        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiresAt)
                return _accessToken;

            using var response = await _http.PostAsJsonAsync(
                "api/v1/auth/universal-auth/login",
                new UniversalAuthLoginRequest(_options.ClientId, _options.ClientSecret),
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError(
                    "Infisical Universal Auth falló: {StatusCode} {Body}",
                    (int)response.StatusCode,
                    Truncate(body, 200));
                throw new InvalidOperationException(
                    $"Autenticación Universal Auth contra Infisical falló ({(int)response.StatusCode}).");
            }

            var login = await response.Content.ReadFromJsonAsync<UniversalAuthLoginResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (login is null || string.IsNullOrWhiteSpace(login.AccessToken))
                throw new InvalidOperationException("Infisical no devolvió accessToken.");

            var skew = TimeSpan.FromSeconds(Math.Max(0, _options.TokenCacheSkewSeconds));
            var ttl = TimeSpan.FromSeconds(Math.Max(60, login.ExpiresIn)) - skew;

            _accessToken = login.AccessToken;
            _tokenExpiresAt = DateTimeOffset.UtcNow.Add(ttl);

            _logger.LogInformation(
                "Infisical accessToken renovado; válido ~{Minutes} min",
                (int)ttl.TotalMinutes);

            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private void ValidateConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException("Infisical:BaseUrl no está configurado.");
        if (string.IsNullOrWhiteSpace(_options.WorkspaceId))
            throw new InvalidOperationException("Infisical:WorkspaceId no está configurado.");
        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new InvalidOperationException(
                "Infisical:ClientId / ClientSecret no configurados. Use variables de entorno Infisical__ClientId e Infisical__ClientSecret.");
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";

    private sealed record UniversalAuthLoginRequest(
        [property: JsonPropertyName("clientId")] string ClientId,
        [property: JsonPropertyName("clientSecret")] string ClientSecret);

    private sealed class UniversalAuthLoginResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("tokenType")]
        public string TokenType { get; set; } = "Bearer";
    }

    private sealed class InfisicalSecretResponse
    {
        [JsonPropertyName("secret")]
        public InfisicalSecretItem? Secret { get; set; }

        [JsonPropertyName("secrets")]
        public List<InfisicalSecretItem>? Secrets { get; set; }
    }

    private sealed class InfisicalSecretItem
    {
        [JsonPropertyName("secretKey")]
        public string SecretKey { get; set; } = string.Empty;

        [JsonPropertyName("secretValue")]
        public string SecretValue { get; set; } = string.Empty;
    }
}
