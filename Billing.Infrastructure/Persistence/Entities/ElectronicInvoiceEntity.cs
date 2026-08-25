namespace Ecunexo.Billing.Infrastructure.Persistence.Entities;

public sealed class ElectronicInvoiceEntity
{
    public Guid Id { get; set; }
    public Guid EmitterId { get; set; }
    public Guid? TenantId { get; set; }
    public string EmitterRuc { get; set; } = string.Empty;
    public string Establishment { get; set; } = string.Empty;
    public string EmissionPoint { get; set; } = string.Empty;
    public string Sequential { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "01";
    public DateOnly IssueDate { get; set; }
    public string? AccessKey { get; set; }
    public string State { get; set; } = "Draft";
    public string CounterpartyJson { get; set; } = "{}";
    public string PaymentFormCode { get; set; } = "01";
    public string? AdditionalNote { get; set; }
    public int PaymentTermDays { get; set; }
    public Guid? ModifiedInvoiceId { get; set; }
    public string? ModifiedDocumentType { get; set; }
    public string? ModifiedDocumentNumber { get; set; }
    public DateOnly? ModifiedIssueDate { get; set; }
    public string? Motivo { get; set; }
    public decimal SubtotalWithoutTax { get; set; }
    public decimal GrandTotal { get; set; }
    public string TaxTotalsJson { get; set; } = "[]";
    public string? SriTransmissionState { get; set; }
    public string? SriMessagesJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public EmitterEntity Emitter { get; set; } = null!;
    public List<InvoiceLineEntity> Lines { get; set; } = [];
    public InvoiceXmlArtifactEntity? XmlArtifact { get; set; }
}
