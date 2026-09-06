using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents;
using Ecunexo.Billing.Core.Documents.Ports;
using Ecunexo.Billing.Core.Sri;

namespace Ecunexo.Billing.Business.Tests.Support;

internal sealed class FakeInvoiceRepository : IInvoiceRepository
{
    public List<(SalesDocument Document, Guid EmitterId, Guid? TenantId, Guid? CreatedByUserId)> Saved { get; } = [];

    public Task SaveNewAsync(
        SalesDocument document,
        Guid emitterId,
        Guid? tenantId,
        Guid? createdByUserId = null,
        CancellationToken cancellationToken = default)
    {
        Saved.Add((document, emitterId, tenantId, createdByUserId));
        return Task.CompletedTask;
    }

    public Task<Guid?> GetCreatedByUserIdAsync(Guid invoiceId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<SalesDocument?> GetDomainAsync(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<(SalesDocument Document, Guid? TenantId)?> GetDomainWithTenantAsync(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<(SalesDocument Document, byte[]? UnsignedXml, byte[]? SignedXml)?> GetWithXmlAsync(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task UpdateAfterSignAsync(
        ElectronicDocument document,
        byte[] unsignedXml,
        byte[] signedXml,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task UpdateStateAsync(
        Guid invoiceId,
        SriDocumentState state,
        SriTransmissionResult? transmission,
        string? authorizedXml = null,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<InvoiceListResult> ListAsync(InvoiceListQuery query, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Guid?> GetNextResendableInvoiceIdAsync(
        Guid emitterId,
        string establishment,
        string emissionPoint,
        string documentType,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Guid?> FindBlockingCreditNoteIdAsync(Guid modifiedInvoiceId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<byte[]?> GetSignedXmlAsync(Guid invoiceId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<SriMessageResponseDto>> GetMessagesAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<string?> GetTransmissionStateAsync(Guid invoiceId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<DateTimeOffset?> GetAuthorizationDateAsync(Guid invoiceId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
