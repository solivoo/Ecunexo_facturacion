using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents;
using Ecunexo.Billing.Domain.Tests.TaxCatalog;

namespace Ecunexo.Billing.Domain.Tests.Documents;

public class ElectronicCreditNoteTests
{
    [Fact(DisplayName = "NC se crea desde factura autorizada con tipo 04")]
    public void CreateFromAuthorizedInvoice_CopiesTotalsAndReference()
    {
        var invoice = BuildAuthorizedInvoice();
        var note = ElectronicCreditNote.CreateFromAuthorizedInvoice(
            invoice,
            SequentialNumber.Create("000000001"),
            new DateOnly(2024, 2, 1),
            "Anulación total");

        Assert.Equal("04", note.DocumentType.Value);
        Assert.Equal(SriDocumentState.Draft, note.State);
        Assert.Equal(invoice.Id, note.ModifiedInvoiceId);
        Assert.Equal("001-001-000000001", note.ModifiedDocumentNumber);
        Assert.Equal(invoice.IssueDate, note.ModifiedIssueDate);
        Assert.Equal(invoice.GrandTotal.Amount, note.GrandTotal.Amount);
        Assert.Equal("Anulación total", note.Motivo);
        Assert.Single(note.Lines);
    }

    [Fact(DisplayName = "NC sobre borrador es rechazada")]
    public void CreateFromDraft_Throws()
    {
        var invoice = BuildDraftInvoice();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ElectronicCreditNote.CreateFromAuthorizedInvoice(
                invoice,
                SequentialNumber.Create("000000001"),
                new DateOnly(2024, 2, 1),
                "Anulación"));

        Assert.Contains("autorizada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Motivo vacío es rechazado")]
    public void Create_EmptyMotivo_Throws()
    {
        var invoice = BuildAuthorizedInvoice();

        Assert.Throws<ArgumentException>(() =>
            ElectronicCreditNote.CreateFromAuthorizedInvoice(
                invoice,
                SequentialNumber.Create("000000001"),
                new DateOnly(2024, 2, 1),
                "  "));
    }

    [Fact(DisplayName = "Fecha anterior a la factura es rechazada")]
    public void Create_EarlierIssueDate_Throws()
    {
        var invoice = BuildAuthorizedInvoice();

        Assert.Throws<ArgumentException>(() =>
            ElectronicCreditNote.CreateFromAuthorizedInvoice(
                invoice,
                SequentialNumber.Create("000000001"),
                new DateOnly(2023, 1, 1),
                "Anulación"));
    }

    private static ElectronicInvoice BuildDraftInvoice()
    {
        var line = new InvoiceLine(
            1,
            "Servicio",
            2m,
            new Money(50m),
            Money.Zero,
            new Money(100m),
            [new LineTax("2", "4", 15m, new Money(100m), new Money(15m))]);

        return ElectronicInvoice.Create(
            Ruc.Create("1792146739001"),
            EstablishmentCode.Create("001"),
            EmissionPoint.Create("001"),
            SequentialNumber.Create("000000001"),
            new DateOnly(2024, 1, 21),
            Counterparty.Create("04", "0999999999001", "Cliente Prueba"),
            [line],
            TestTaxRateCatalog.WithIva15());
    }

    private static ElectronicInvoice BuildAuthorizedInvoice()
    {
        var invoice = BuildDraftInvoice();
        var key = ClaveAcceso.Create(new ClaveAccesoComponents(
            invoice.IssueDate,
            invoice.DocumentType,
            invoice.EmitterRuc,
            "1",
            invoice.Establishment,
            invoice.EmissionPoint,
            invoice.Sequential,
            12345678,
            "1"));
        invoice.MarkSigned(key);
        invoice.MarkReceived();
        invoice.MarkProcessing();
        invoice.MarkAuthorized();
        return invoice;
    }
}
