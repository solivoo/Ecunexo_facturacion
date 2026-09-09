using System.Text.Json;
using Ecunexo.Billing.Infrastructure.Ride;

namespace Ecunexo.Billing.Api.Ride;

public sealed class RideProviderFileStore(IConfiguration configuration, IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Ruta escribible en contenedor (volumen /data). Evita ContentRoot de solo lectura con USER no-root.
    /// </summary>
    public string FilePath
    {
        get
        {
            var configured = configuration["RideProvider:DataFilePath"]
                ?? configuration["RIDE_PROVIDER_FILE_PATH"];
            if (!string.IsNullOrWhiteSpace(configured))
                return configured.Trim();

            // Preferencia producción Docker: /data montado en compose.
            if (Directory.Exists("/data") || environment.IsProduction())
                return "/data/ride-provider.json";

            return Path.Combine(environment.ContentRootPath, "Data", "ride-provider.json");
        }
    }

    public async Task SaveAsync(RideProviderOptions value, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(value);

        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var payload = new Dictionary<string, RideProviderOptions>
        {
            [RideProviderOptions.SectionName] = value,
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var tempPath = FilePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
        File.Move(tempPath, FilePath, overwrite: true);
    }
}
