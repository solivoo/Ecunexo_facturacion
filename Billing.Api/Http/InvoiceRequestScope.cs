using Ecunexo.Billing.Domain.Documents;

namespace Ecunexo.Billing.Api.Http;

internal static class InvoiceRequestScope
{
    public const string UserIdHeader = "X-User-Id";
    public const string TenantIdHeader = "X-Tenant-Id";
    public const string ReadScopeHeader = "X-Invoice-Read-Scope";

    public static InvoiceViewerScope From(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var header = request.Headers[ReadScopeHeader].FirstOrDefault();
        var query = request.Query["scope"].FirstOrDefault();
        var canReadAll = IsAll(header) || IsAll(query);
        return new InvoiceViewerScope(ParseGuidHeader(request, UserIdHeader), canReadAll);
    }

    public static Guid? ReadTenantId(HttpRequest request) =>
        ParseGuidHeader(request, TenantIdHeader);

    public static Guid? ReadUserId(HttpRequest request) =>
        ParseGuidHeader(request, UserIdHeader);

    private static bool IsAll(string? value) =>
        string.Equals(value, "all", StringComparison.OrdinalIgnoreCase);

    private static Guid? ParseGuidHeader(HttpRequest request, string name) =>
        request.Headers.TryGetValue(name, out var values)
        && Guid.TryParse(values.FirstOrDefault(), out var id)
            ? id
            : null;
}
