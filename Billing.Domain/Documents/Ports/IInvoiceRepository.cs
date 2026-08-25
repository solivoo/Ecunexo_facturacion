using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents;
using Ecunexo.Billing.Domain.Sri;
using EmitterAgg = Ecunexo.Billing.Domain.Emitter.Emitter;

namespace Ecunexo.Billing.Domain.Documents.Ports;

public sealed record InvoiceListQuery(
    Guid EmitterId,
    DateOnly? From = null,
    DateOnly? To = null,
    string? State = null,
    int Page = 1,
    int PageSize = 50,
    Guid? CreatedByUserId = null);

public sealed record InvoiceListItem(
    Guid InvoiceId,
    DateOnly IssueDate,
    string Establishment,
    string EmissionPoint,
    string Sequential,
    string? AccessKey,
    string CounterpartyName,
    string CounterpartyIdentification,
    decimal GrandTotal,
    string State,
    string? SriTransmissionState,
    DateTimeOffset CreatedAt,
    bool CanResend,
    string DocumentType = "01",
    bool CanVoid = false,
    bool IsVoided = false,
    Guid? ModifiedInvoiceId = null,
    string CounterpartyIdentificationType = "04",
    string? VoidPath = null,
    DateOnly? VoidDeadline = null,
    string? VoidMessage = null);

public sealed record InvoiceListResult(
    IReadOnlyList<InvoiceListItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public interface IInvoiceRepository
{
    Task SaveNewAsync(
        SalesDocument document,
        Guid emitterId,
        Guid? tenantId,
        Guid? createdByUserId = null,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetCreatedByUserIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<SalesDocument?> GetDomainAsync(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    /// <summary>Dominio + TenantId persistido (para egreso inventario post-autorización).</summary>
    Task<(SalesDocument Document, Guid? TenantId)?> GetDomainWithTenantAsync(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<(SalesDocument Document, byte[]? UnsignedXml, byte[]? SignedXml)?> GetWithXmlAsync(
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task UpdateAfterSignAsync(
        ElectronicDocument document,
        byte[] unsignedXml,
        byte[] signedXml,
        CancellationToken cancellationToken = default);

    Task UpdateStateAsync(
        Guid invoiceId,
        SriDocumentState state,
        SriTransmissionResult? transmission,
        string? authorizedXml = null,
        CancellationToken cancellationToken = default);

    Task<InvoiceListResult> ListAsync(
        InvoiceListQuery query,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetNextResendableInvoiceIdAsync(
        Guid emitterId,
        string establishment,
        string emissionPoint,
        string documentType,
        CancellationToken cancellationToken = default);

    Task<Guid?> FindBlockingCreditNoteIdAsync(
        Guid modifiedInvoiceId,
        CancellationToken cancellationToken = default);

    Task<byte[]?> GetSignedXmlAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SriMessageResponseDto>> GetMessagesAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<string?> GetTransmissionStateAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    Task<DateTimeOffset?> GetAuthorizationDateAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}

public sealed record SriMessageResponseDto(string Identifier, string Text, string? Detail, string Type);

public interface IEmitterRepository
{
    Task<EmitterAgg?> GetAsync(Guid emitterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Emisor preferido para un RUC: match por tenant, luego el que más facturas tenga, luego el más antiguo.
    /// Si hay tenant y el emisor no lo tiene, lo asocia.
    /// </summary>
    Task<Guid?> FindPreferredIdByRucAsync(
        string ruc,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    Task SyncIdentityAsync(
        Guid emitterId,
        string businessName,
        string mainAddress,
        string? tradeName,
        CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(
        EmitterAgg emitter,
        Guid? tenantId,
        string defaultEstablishmentCode,
        string defaultEmissionPoint,
        CancellationToken cancellationToken = default);

    Task SaveCertificateAsync(EmitterAgg emitter, CancellationToken cancellationToken = default);

    Task EnsureEstablishmentPointAsync(
        Guid emitterId,
        string establishmentCode,
        string emissionPoint,
        string documentType,
        string address,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resuelve estab/pto para emitir factura: solo puntos ya registrados en BD.
    /// Si el cliente envía valores, deben existir; si no, usa el punto por defecto (primer config factura).
    /// </summary>
    Task<(string Establishment, string EmissionPoint)> ResolveRegisteredFacturaPointAsync(
        Guid emitterId,
        string? requestedEstablishment,
        string? requestedEmissionPoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Candado: SELECT FOR UPDATE + incrementa last_sequential.
    /// Si <paramref name="requestedSequential"/> es mayor al próximo automático, salta el contador hasta ese valor.
    /// </summary>
    Task<SequentialNumber> AllocateNextSequentialAsync(
        Guid emitterId,
        string establishmentCode,
        string emissionPoint,
        string documentType,
        string? requestedSequential = null,
        CancellationToken cancellationToken = default);

    Task<string> PeekNextSequentialAsync(
        Guid emitterId,
        string establishmentCode,
        string emissionPoint,
        string documentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Configura el próximo secuencial a emitir (LastSequential = next - 1).
    /// No permite rebobinar por debajo del último ya asignado.
    /// </summary>
    Task<string> SetNextSequentialAsync(
        Guid emitterId,
        string establishmentCode,
        string emissionPoint,
        string documentType,
        string nextSequential,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(string Code, string Address)>> ListEstablishmentsAsync(
        Guid emitterId,
        CancellationToken cancellationToken = default);

    Task AddEstablishmentAsync(
        Guid emitterId,
        string code,
        string address,
        string emissionPoint,
        string documentType,
        CancellationToken cancellationToken = default);
}

public interface ISriOutboxRepository
{
    Task EnqueueAsync(
        Guid invoiceId,
        Guid emitterId,
        string operation,
        string environment,
        string? accessKey,
        DateTimeOffset nextAttemptAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SriOutboxWorkItem>> ClaimPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(Guid outboxId, CancellationToken cancellationToken = default);

    Task MarkRetryAsync(
        Guid outboxId,
        DateTimeOffset nextAttemptAt,
        string error,
        CancellationToken cancellationToken = default);

    Task MarkDeadAsync(Guid outboxId, string error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reencola la operación (resetea intentos). Si no existe fila, la crea.
    /// </summary>
    Task RequeueAsync(
        Guid invoiceId,
        Guid emitterId,
        string operation,
        string environment,
        string? accessKey,
        CancellationToken cancellationToken = default);
}

public sealed record SriOutboxWorkItem(
    Guid OutboxId,
    Guid InvoiceId,
    Guid EmitterId,
    string Operation,
    string Environment,
    string? AccessKey,
    int AttemptCount);
