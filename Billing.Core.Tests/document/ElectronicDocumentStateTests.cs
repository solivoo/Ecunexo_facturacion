using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents;
using Ecunexo.Billing.Core.Tests.TaxCatalog;

namespace Ecunexo.Billing.Core.Tests.Documents;

public class ElectronicDocumentStateTests
{
    private static ElectronicInvoice CreateDraftInvoice()
    {
        var line = new InvoiceLine(
            1,
            "Producto prueba",
            1m,
            new Money(100m),
            Money.Zero,
            new Money(100m),
            []);

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

    [Fact(DisplayName = "Factura nueva nace en estado Draft")]
    public void Create_ShouldStartInDraft()
    {
        var invoice = CreateDraftInvoice();

        Assert.Equal(SriDocumentState.Draft, invoice.State);
        Assert.Null(invoice.AccessKey);
    }

    [Fact(DisplayName = "MarkSigned asigna clave y pasa a Signed")]
    public void MarkSigned_FromDraft_ShouldSetAccessKeyAndSigned()
    {
        var invoice = CreateDraftInvoice();
        var components = new ClaveAccesoComponents(
            invoice.IssueDate,
            DocumentTypeCode.Factura,
            invoice.EmitterRuc,
            "1",
            invoice.Establishment,
            invoice.EmissionPoint,
            invoice.Sequential,
            12345678,
            "1");
        var clave = ClaveAcceso.Create(components);

        invoice.MarkSigned(clave);

        Assert.Equal(SriDocumentState.Signed, invoice.State);
        Assert.Equal(clave.Value, invoice.AccessKey!.Value);
    }

    [Fact(DisplayName = "No se puede firmar si no está en Draft")]
    public void MarkSigned_WhenNotDraft_ShouldThrow()
    {
        var invoice = CreateDraftInvoice();
        var clave = ClaveAcceso.Create(new ClaveAccesoComponents(
            invoice.IssueDate,
            DocumentTypeCode.Factura,
            invoice.EmitterRuc,
            "1",
            invoice.Establishment,
            invoice.EmissionPoint,
            invoice.Sequential,
            12345678,
            "1"));
        invoice.MarkSigned(clave);

        Assert.Throws<InvalidOperationException>(() => invoice.MarkSigned(clave));
    }


    [Fact(DisplayName = "MarkAuthorized solo desde Processing")]
    public void MarkAuthorized_FromProcessing_ShouldSucceed()
    {
        var invoice = CreateDraftInvoice();
        var clave = ClaveAcceso.Create(new ClaveAccesoComponents(
            invoice.IssueDate,
            DocumentTypeCode.Factura,
            invoice.EmitterRuc,
            "1",
            invoice.Establishment,
            invoice.EmissionPoint,
            invoice.Sequential,
            12345678,
            "1"));

        invoice.MarkSigned(clave);
        invoice.MarkReceived();
        invoice.MarkProcessing();
        invoice.MarkAuthorized();

        Assert.Equal(SriDocumentState.Authorized, invoice.State);
    }
}