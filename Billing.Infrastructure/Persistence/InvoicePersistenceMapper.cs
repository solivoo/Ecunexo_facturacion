using System.Text.Json;
using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents;
using Ecunexo.Billing.Infrastructure.Persistence.Entities;

namespace Ecunexo.Billing.Infrastructure.Persistence;

internal static class InvoicePersistenceMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static ElectronicInvoiceEntity ToEntity(
        SalesDocument document,
        Guid emitterId,
        Guid? tenantId,
        Guid? createdByUserId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var paymentTermDays = document is ElectronicInvoice invoice ? invoice.PaymentTermDays : 0;
        var note = document as ElectronicCreditNote;

        return new ElectronicInvoiceEntity
        {
            Id = document.Id,
            EmitterId = emitterId,
            TenantId = tenantId,
            EmitterRuc = document.EmitterRuc.Value,
            Establishment = document.Establishment.Value,
            EmissionPoint = document.EmissionPoint.Value,
            Sequential = document.Sequential.Value,
            DocumentType = document.DocumentType.Value,
            IssueDate = document.IssueDate,
            AccessKey = document.AccessKey?.Value,
            State = document.State.ToString(),
            CounterpartyJson = JsonSerializer.Serialize(
                new CounterpartyDto(
                    document.Counterparty.IdentificationType,
                    document.Counterparty.Identification,
                    document.Counterparty.BusinessName,
                    document.Counterparty.Address,
                    document.Counterparty.Email,
                    document.Counterparty.Phone),
                JsonOptions),
            PaymentFormCode = document.PaymentFormCode,
            AdditionalNote = document.AdditionalNote,
            PaymentTermDays = paymentTermDays,
            ModifiedInvoiceId = note?.ModifiedInvoiceId,
            ModifiedDocumentType = note?.ModifiedDocumentType,
            ModifiedDocumentNumber = note?.ModifiedDocumentNumber,
            ModifiedIssueDate = note?.ModifiedIssueDate,
            Motivo = note?.Motivo,
            SubtotalWithoutTax = document.SubtotalWithoutTax.Amount,
            GrandTotal = document.GrandTotal.Amount,
            TaxTotalsJson = JsonSerializer.Serialize(
                document.TaxTotals.Select(t => new TaxTotalDto(t.TaxCode, t.RateCode, t.TaxableBase.Amount, t.Value.Amount)),
                JsonOptions),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = createdByUserId,
            Lines = document.Lines.Select(l => new InvoiceLineEntity
            {
                Id = Guid.CreateVersion7(),
                InvoiceId = document.Id,
                LineNumber = l.LineNumber,
                MainCode = l.MainCode,
                CatalogItemId = l.CatalogItemId,
                ItemKind = l.ItemKind,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice.Amount,
                Discount = l.Discount.Amount,
                LineTotalWithoutTax = l.LineTotalWithoutTax.Amount,
                TaxesJson = JsonSerializer.Serialize(
                    l.Taxes.Select(t => new LineTaxDto(
                        t.TaxCode,
                        t.RateCode,
                        t.Rate,
                        t.TaxableBase.Amount,
                        t.Value.Amount)),
                    JsonOptions),
            }).ToList(),
            XmlArtifact = new InvoiceXmlArtifactEntity { InvoiceId = document.Id },
        };
    }

    public static SalesDocument ToDomain(ElectronicInvoiceEntity entity)
    {
        var cp = JsonSerializer.Deserialize<CounterpartyDto>(entity.CounterpartyJson, JsonOptions)
            ?? throw new InvalidOperationException("Counterparty JSON inválido.");
        var taxTotals = JsonSerializer.Deserialize<List<TaxTotalDto>>(entity.TaxTotalsJson, JsonOptions) ?? [];
        var lines = entity.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l =>
            {
                var taxes = JsonSerializer.Deserialize<List<LineTaxDto>>(l.TaxesJson, JsonOptions) ?? [];
                return new InvoiceLine(
                    l.LineNumber,
                    l.Description,
                    l.Quantity,
                    new Money(l.UnitPrice),
                    new Money(l.Discount),
                    new Money(l.LineTotalWithoutTax),
                    taxes.Select(t => new LineTax(
                        t.TaxCode,
                        t.RateCode,
                        t.Rate,
                        new Money(t.TaxableBase),
                        new Money(t.Value))).ToList(),
                    l.MainCode,
                    l.CatalogItemId,
                    l.ItemKind);
            })
            .ToList();

        ClaveAcceso? key = null;
        if (!string.IsNullOrWhiteSpace(entity.AccessKey))
            key = ClaveAcceso.FromExisting(entity.AccessKey);

        var counterparty = Counterparty.Create(
            cp.IdentificationType,
            cp.Identification,
            cp.BusinessName,
            cp.Address,
            cp.Email,
            cp.Phone);
        var mappedTaxTotals = taxTotals.Select(t => new DocumentTaxTotal(
            t.TaxCode,
            t.RateCode,
            new Money(t.TaxableBase),
            new Money(t.Value))).ToList();

        if (entity.DocumentType == DocumentTypeCode.NotaCredito.Value)
        {
            if (entity.ModifiedInvoiceId is null
                || string.IsNullOrWhiteSpace(entity.ModifiedDocumentType)
                || string.IsNullOrWhiteSpace(entity.ModifiedDocumentNumber)
                || entity.ModifiedIssueDate is null
                || string.IsNullOrWhiteSpace(entity.Motivo))
            {
                throw new InvalidOperationException("La nota de crédito no tiene el documento modificado completo.");
            }

            return ElectronicCreditNote.Rehydrate(
                entity.Id,
                Ruc.Create(entity.EmitterRuc),
                EstablishmentCode.Create(entity.Establishment),
                EmissionPoint.Create(entity.EmissionPoint),
                SequentialNumber.Create(entity.Sequential),
                entity.IssueDate,
                counterparty,
                lines,
                new Money(entity.SubtotalWithoutTax),
                mappedTaxTotals,
                new Money(entity.GrandTotal),
                Enum.Parse<SriDocumentState>(entity.State),
                key,
                entity.ModifiedInvoiceId.Value,
                entity.ModifiedDocumentType,
                entity.ModifiedDocumentNumber,
                entity.ModifiedIssueDate.Value,
                entity.Motivo,
                entity.PaymentFormCode,
                entity.AdditionalNote);
        }

        return ElectronicInvoice.Rehydrate(
            entity.Id,
            Ruc.Create(entity.EmitterRuc),
            EstablishmentCode.Create(entity.Establishment),
            EmissionPoint.Create(entity.EmissionPoint),
            SequentialNumber.Create(entity.Sequential),
            entity.IssueDate,
            counterparty,
            lines,
            new Money(entity.SubtotalWithoutTax),
            mappedTaxTotals,
            new Money(entity.GrandTotal),
            Enum.Parse<SriDocumentState>(entity.State),
            key,
            entity.PaymentFormCode,
            entity.AdditionalNote,
            entity.PaymentTermDays);
    }

    private sealed record CounterpartyDto(
        string IdentificationType,
        string Identification,
        string BusinessName,
        string? Address,
        string? Email = null,
        string? Phone = null);

    private sealed record TaxTotalDto(string TaxCode, string RateCode, decimal TaxableBase, decimal Value);

    private sealed record LineTaxDto(
        string TaxCode,
        string RateCode,
        decimal Rate,
        decimal TaxableBase,
        decimal Value);
}
