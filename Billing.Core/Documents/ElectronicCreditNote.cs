namespace Ecunexo.Billing.Core.Documents;

using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents.Services;
using Ecunexo.Billing.Core.TaxCatalog;

public sealed class ElectronicCreditNote : SalesDocument
{
    public override DocumentTypeCode DocumentType => DocumentTypeCode.NotaCredito;

    public Guid ModifiedInvoiceId { get; private set; }
    public string ModifiedDocumentType { get; private set; } = DocumentTypeCode.Factura.Value;
    public string ModifiedDocumentNumber { get; private set; } = string.Empty;
    public DateOnly ModifiedIssueDate { get; private set; }
    public string Motivo { get; private set; } = string.Empty;

    private ElectronicCreditNote() { }

    public static ElectronicCreditNote CreateFromAuthorizedInvoice(
        ElectronicInvoice invoice,
        SequentialNumber sequential,
        DateOnly issueDate,
        string motivo)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(sequential);

        if (invoice.State != SriDocumentState.Authorized)
        {
            throw new InvalidOperationException(
                "Solo se puede emitir nota de crédito sobre una factura autorizada por el SRI.");
        }

        if (invoice.DocumentType.Value != DocumentTypeCode.Factura.Value)
        {
            throw new InvalidOperationException("La nota de crédito solo puede referenciar una factura (01).");
        }

        if (issueDate < invoice.IssueDate)
        {
            throw new ArgumentException(
                "La fecha de la nota de crédito no puede ser anterior a la de la factura.",
                nameof(issueDate));
        }

        var lines = invoice.Lines.Select(CloneLine).ToList();
        var (subtotal, taxTotals, grandTotal) = InvoiceTotalsCalculator.Calculate(lines);

        var note = new ElectronicCreditNote
        {
            Id = Guid.CreateVersion7(),
            EmitterRuc = invoice.EmitterRuc,
            Establishment = invoice.Establishment,
            EmissionPoint = invoice.EmissionPoint,
            Sequential = sequential,
            IssueDate = issueDate,
            Counterparty = invoice.Counterparty,
            Lines = lines,
            SubtotalWithoutTax = subtotal,
            TaxTotals = taxTotals,
            GrandTotal = grandTotal,
            PaymentFormCode = invoice.PaymentFormCode,
            AdditionalNote = invoice.AdditionalNote,
            State = SriDocumentState.Draft,
            ModifiedInvoiceId = invoice.Id,
            ModifiedDocumentType = DocumentTypeCode.Factura.Value,
            ModifiedDocumentNumber =
                $"{invoice.Establishment.Value}-{invoice.EmissionPoint.Value}-{invoice.Sequential.Value}",
            ModifiedIssueDate = invoice.IssueDate,
            Motivo = NormalizeMotivo(motivo),
        };

        InvoiceTotalsValidator.Validate(lines, subtotal, taxTotals, grandTotal);
        return note;
    }

    public static ElectronicCreditNote Rehydrate(
        Guid id,
        Ruc emitterRuc,
        EstablishmentCode estab,
        EmissionPoint ptoEmi,
        SequentialNumber sequential,
        DateOnly issueDate,
        Counterparty counterparty,
        IReadOnlyList<InvoiceLine> lines,
        Money subtotal,
        IReadOnlyList<DocumentTaxTotal> taxTotals,
        Money grandTotal,
        SriDocumentState state,
        ClaveAcceso? accessKey,
        Guid modifiedInvoiceId,
        string modifiedDocumentType,
        string modifiedDocumentNumber,
        DateOnly modifiedIssueDate,
        string motivo,
        string? paymentFormCode = null,
        string? additionalNote = null)
    {
        return new ElectronicCreditNote
        {
            Id = id,
            EmitterRuc = emitterRuc,
            Establishment = estab,
            EmissionPoint = ptoEmi,
            Sequential = sequential,
            IssueDate = issueDate,
            Counterparty = counterparty,
            Lines = lines,
            SubtotalWithoutTax = subtotal,
            TaxTotals = taxTotals,
            GrandTotal = grandTotal,
            PaymentFormCode = PaymentForm.FromCode(paymentFormCode).Code,
            AdditionalNote = additionalNote,
            State = state,
            AccessKey = accessKey,
            ModifiedInvoiceId = modifiedInvoiceId,
            ModifiedDocumentType = modifiedDocumentType,
            ModifiedDocumentNumber = modifiedDocumentNumber,
            ModifiedIssueDate = modifiedIssueDate,
            Motivo = motivo,
        };
    }

    public static bool BlocksNewCreditNote(SriDocumentState state) =>
        state is not (SriDocumentState.Returned or SriDocumentState.NotAuthorized);

    public static bool BlocksNewCreditNote(string state) =>
        Enum.TryParse<SriDocumentState>(state, ignoreCase: true, out var parsed)
        && BlocksNewCreditNote(parsed);

    private static string NormalizeMotivo(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de la nota de crédito es obligatorio.", nameof(motivo));

        var trimmed = motivo.Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (trimmed.Length is < 1 or > 300)
        {
            throw new ArgumentException("El motivo debe tener entre 1 y 300 caracteres.", nameof(motivo));
        }

        return trimmed;
    }

    private static InvoiceLine CloneLine(InvoiceLine line) =>
        new(
            line.LineNumber,
            line.Description,
            line.Quantity,
            line.UnitPrice,
            line.Discount,
            line.LineTotalWithoutTax,
            line.Taxes.ToList(),
            line.MainCode,
            line.CatalogItemId,
            line.ItemKind);
}
