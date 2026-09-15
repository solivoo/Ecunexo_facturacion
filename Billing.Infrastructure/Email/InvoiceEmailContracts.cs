namespace Ecunexo.Billing.Infrastructure.Email;

public sealed class InvoiceEmailOptions
{
    public const string SectionName = "InvoiceEmail";

    public bool Enabled { get; set; }

    /// <summary>Base URL de ecunexo_api, p. ej. http://ecunexo-api:8080</summary>
    public string? BaseUrl { get; set; }

    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 30;
}

public sealed record InvoiceEmailNotifyRequest(
    Guid BillingInvoiceId,
    string CounterpartyEmail,
    string CounterpartyName,
    string DocumentType,
    string SerieSecuencial,
    string? AccessKey,
    decimal GrandTotal);

public interface IInvoiceEmailNotifier
{
    Task NotifyAuthorizedAsync(
        Guid tenantId,
        InvoiceEmailNotifyRequest request,
        CancellationToken cancellationToken = default);
}
