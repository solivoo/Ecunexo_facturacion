using Ecunexo.Billing.Core.Sri;

namespace Ecunexo.Billing.Infrastructure.Sri;

public sealed class SriOptions
{
    public const string SectionName = "Sri";

    /// <summary>
    /// Si es true, se usa <see cref="Adapters.InMemorySriGateway"/> (sin HTTP al SRI).
    /// </summary>
    public bool UseInMemoryGateway { get; set; } = true;

    /// <summary>Ambiente por defecto para recepción/autorización (Test | Production).</summary>
    public string DefaultEnvironment { get; set; } = nameof(SriEnvironment.Test);

    /// <summary>
    /// Segundos de espera recomendados por el SRI entre recepción RECIBIDA y consulta de autorización.
    /// </summary>
    public int AuthorizationPollDelaySeconds { get; set; } = 3;

    public int HttpTimeoutSeconds { get; set; } = 60;

    public int WorkerPoolSize { get; set; } = 2;
    public int MaxConcurrentCallsPerEmitter { get; set; } = 2;
    public int OutboxBatchSize { get; set; } = 10;
    /// <summary>
    /// Intervalo del worker outbox. 5s equilibra latencia post-firma vs carga/ruido en BD/consola.
    /// </summary>
    public int OutboxPollSeconds { get; set; } = 5;
    public int MaxOutboxAttempts { get; set; } = 8;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerBreakSeconds { get; set; } = 60;
    public int[] RetryBackoffSeconds { get; set; } = [5, 15, 45, 120, 300];

    public SriEndpointOptions Test { get; set; } = new()
    {
        ReceptionUrl =
            "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline",
        AuthorizationUrl =
            "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline",
    };

    public SriEndpointOptions Production { get; set; } = new()
    {
        ReceptionUrl =
            "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline",
        AuthorizationUrl =
            "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline",
    };

    /// <summary>
    /// Identidad inscrita en celcer. En Test, el XML usa esto si el RUC del tenant no existe en el SRI.
    /// </summary>
    public SriTestEmissionOptions TestEmission { get; set; } = new();

    public SriEnvironment ResolveEnvironment() =>
        Enum.TryParse<SriEnvironment>(DefaultEnvironment, ignoreCase: true, out var env)
            ? env
            : SriEnvironment.Test;

    public SriEndpointOptions ResolveEndpoints(SriEnvironment environment) =>
        environment == SriEnvironment.Production ? Production : Test;
}

public sealed class SriEndpointOptions
{
    public string ReceptionUrl { get; set; } = string.Empty;
    public string AuthorizationUrl { get; set; } = string.Empty;
}

/// <summary>RUC y ficha del contribuyente habilitado en ambiente de pruebas SRI.</summary>
public sealed class SriTestEmissionOptions
{
    public string Ruc { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string MainAddress { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string Establishment { get; set; } = "002";
    public string EmissionPoint { get; set; } = "001";

    public bool IsConfigured()
    {
        var ruc = new string(Ruc.Where(char.IsDigit).ToArray());
        return ruc.Length == 13
            && !string.IsNullOrWhiteSpace(BusinessName)
            && !string.IsNullOrWhiteSpace(MainAddress);
    }
}
