using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents;

namespace Ecunexo.Billing.Domain.Tests.Documents;

/// <summary>Fase 4 — snapshot de catálogo en línea (sin FK cross-host).</summary>
public class InvoiceLineSnapshotTests
{
    [Fact(DisplayName = "Línea conserva CatalogItemId e ItemKind physical")]
    public void Create_WithCatalogSnapshot_PreservesFields()
    {
        // Preparar
        var catalogItemId = Guid.CreateVersion7();

        // Actuar
        var line = new InvoiceLine(
            1,
            "Tornillo M6",
            2m,
            new Money(1.5m),
            Money.Zero,
            new Money(3m),
            [new LineTax("2", "4", 15m, new Money(3m), new Money(0.45m))],
            mainCode: "TOR-M6",
            catalogItemId: catalogItemId,
            itemKind: "Physical");

        // Verificar
        Assert.Equal(catalogItemId, line.CatalogItemId);
        Assert.Equal("physical", line.ItemKind);
        Assert.Equal("TOR-M6", line.MainCode);
    }

    [Fact(DisplayName = "ItemKind inválido se normaliza a null")]
    public void Create_InvalidItemKind_BecomesNull()
    {
        // Preparar / Actuar
        var line = new InvoiceLine(
            1,
            "Servicio",
            1m,
            new Money(10m),
            Money.Zero,
            new Money(10m),
            [],
            itemKind: "bundle");

        // Verificar
        Assert.Null(line.ItemKind);
        Assert.Null(line.CatalogItemId);
    }

    [Fact(DisplayName = "ItemKind service se conserva en minúsculas")]
    public void Create_ServiceKind_Preserved()
    {
        // Actuar
        var line = new InvoiceLine(
            1,
            "Consultoría",
            1m,
            new Money(100m),
            Money.Zero,
            new Money(100m),
            [],
            catalogItemId: Guid.CreateVersion7(),
            itemKind: "SERVICE");

        // Verificar
        Assert.Equal("service", line.ItemKind);
        Assert.NotNull(line.CatalogItemId);
    }
}
