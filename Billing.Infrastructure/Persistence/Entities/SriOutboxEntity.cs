namespace Ecunexo.Billing.Infrastructure.Persistence.Entities;

public sealed class SriOutboxEntity
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid EmitterId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string Environment { get; set; } = "Test";
    public string? AccessKey { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
