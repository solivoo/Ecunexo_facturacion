using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents;

namespace Ecunexo.Billing.Domain.Documents;

public abstract class ElectronicDocument
{
    public Guid Id { get; protected set; }
    public Ruc EmitterRuc { get; protected set; } = null!;
    public EstablishmentCode Establishment { get; protected set; } = null!;
    public EmissionPoint EmissionPoint { get; protected set; } = null!;
    public SequentialNumber Sequential { get; protected set; } = null!;
    public DateOnly IssueDate { get; protected set; }
    public ClaveAcceso? AccessKey { get; protected set; }
    public SriDocumentState State { get; protected set; }

    public abstract DocumentTypeCode DocumentType { get; }

    protected ElectronicDocument() { }

    public void MarkSigned(ClaveAcceso accessKey)
    {
        if (State != SriDocumentState.Draft)
            throw new InvalidOperationException("Solo un borrador puede firmarse");

        AccessKey = accessKey;
        State = SriDocumentState.Signed;
    }

    public void MarkAuthorized()
    {
        if (State != SriDocumentState.Processing)
            throw new InvalidOperationException("Solo un documento en procesamiento puede autorizarse");

        State = SriDocumentState.Authorized;
    }

    public void MarkNotAuthorized()
    {
        if (State != SriDocumentState.Processing)
            throw new InvalidOperationException("Solo un documento en procesamiento puede rechazarse");

        State = SriDocumentState.NotAuthorized;
    }

    public void MarkReceived()
{
    if (State != SriDocumentState.Signed && State != SriDocumentState.PendingReception)
        throw new InvalidOperationException("Solo un documento firmado o pendiente de recepción puede marcarse como recibido");

    State = SriDocumentState.Received;
}

    public void MarkProcessing()
    {
        if (State != SriDocumentState.Received)
            throw new InvalidOperationException("Solo un documento recibido puede pasar a procesamiento");

        State = SriDocumentState.Processing;
    }

    public void MarkReturned()
    {
        if (State is not (SriDocumentState.Signed or SriDocumentState.PendingReception or SriDocumentState.Received))
            throw new InvalidOperationException("Solo un documento enviado puede marcarse como DEVUELTA.");

        State = SriDocumentState.Returned;
    }
}