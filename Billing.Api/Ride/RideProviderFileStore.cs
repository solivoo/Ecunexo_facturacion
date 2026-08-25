using System.Text.Json;
using Ecunexo.Billing.Infrastructure.Ride;

namespace Ecunexo.Billing.Api.Ride;

public sealed class RideProviderFileStore(IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public string FilePath =>
        Path.Combine(environment.ContentRootPath, "Data", "ride-provider.json");

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
        await File.WriteAllTextAsync(FilePath, json, cancellationToken).ConfigureAwait(false);
    }
}
