namespace Ecunexo.Billing.Domain.Documents;

using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents.Services;
using Ecunexo.Billing.Domain.TaxCatalog;
using Ecunexo.Billing.Domain.TaxCatalog.Ports;

public class ElectronicInvoice : SalesDocument
{
    public override DocumentTypeCode DocumentType => DocumentTypeCode.Factura;

    /// <summary>Plazo de pago en días (RIDE). Por defecto 0.</summary>
    public int PaymentTermDays { get; private set; }

    private ElectronicInvoice() { }

    public static ElectronicInvoice Create(
        Ruc emitterRuc,
        EstablishmentCode estab,
        EmissionPoint ptoEmi,
        SequentialNumber sequential,
        DateOnly issueDate,
        Counterparty counterparty,
        IReadOnlyList<InvoiceLine> lines,
        ITaxRateRepository taxRateRepository,
        string? paymentFormCode = null,
        string? additionalNote = null,
        int paymentTermDays = 0)
    {
        ArgumentNullException.ThrowIfNull(taxRateRepository);
        if (lines.Count == 0)
            throw new ArgumentException("La factura debe tener al menos una línea");
        InvoiceLineValidator.ValidateAgainstCatalog(lines, issueDate, taxRateRepository);
        var (subtotal, taxTotals, grandTotal) = InvoiceTotalsCalculator.Calculate(lines);
        var payment = PaymentForm.FromCode(paymentFormCode);

        var invoice = new ElectronicInvoice
        {
            Id = Guid.NewGuid(),
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
            PaymentFormCode = payment.Code,
            AdditionalNote = NormalizeNote(additionalNote),
            PaymentTermDays = NormalizeTermDays(paymentTermDays),
            State = SriDocumentState.Draft
        };

        InvoiceTotalsValidator.Validate(invoice);

        return invoice;
    }

    public static ElectronicInvoice Rehydrate(
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
        string? paymentFormCode = null,
        string? additionalNote = null,
        int paymentTermDays = 0)
    {
        return new ElectronicInvoice
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
            AdditionalNote = NormalizeNote(additionalNote),
            PaymentTermDays = NormalizeTermDays(paymentTermDays),
            State = state,
            AccessKey = accessKey,
        };
    }

    private static string? NormalizeNote(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        if (trimmed.Length > 300)
            throw new ArgumentException("La información adicional no puede superar 300 caracteres.", nameof(value));
        return trimmed;
    }

    private static int NormalizeTermDays(int days)
    {
        if (days < 0)
            throw new ArgumentOutOfRangeException(nameof(days), "El plazo de pago no puede ser negativo.");
        return days;
    }
}