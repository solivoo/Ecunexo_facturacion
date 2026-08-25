namespace Ecunexo.Billing.Infrastructure.Persistence.Entities;

public sealed class InvoiceXmlArtifactEntity
{
    public Guid InvoiceId { get; set; }
    public byte[]? UnsignedXml { get; set; }
    public byte[]? SignedXml { get; set; }
    public string? AuthorizedXml { get; set; }
    public DateTimeOffset? AuthorizationDate { get; set; }

    public ElectronicInvoiceEntity Invoice { get; set; } = null!;
}
