using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents;
using Ecunexo.Billing.Core.Documents.Ports;
using Ecunexo.Billing.Core.Sri.Ports;
using Ecunexo.Billing.Core.TaxCatalog.Ports;

namespace Ecunexo.Billing.Business.Invoices;

public sealed class CreateInvoiceService(
    ITaxRateRepository taxRateRepository,
    IEmitterRepository emitterRepository,
    IInvoiceRepository invoiceRepository,
    ISriEmissionIdentityResolver emissionIdentity) : ICreateInvoiceService
{
    public async Task<CreateInvoiceResult> ExecuteAsync(
        CreateInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        var emitter = await emitterRepository
            .GetAsync(command.EmitterId, cancellationToken)
            .ConfigureAwait(false);
        if (emitter is null)
            return new CreateInvoiceNotFound("Emisor no encontrado. Cree el emisor primero.");

        try
        {
            var identity = emissionIdentity.Resolve(
                emitter,
                command.Establishment,
                command.EmissionPoint);

            string estabCode;
            string ptoCode;
            var sequentialOwnerId = command.EmitterId;
            if (identity.IsSubstituted
                && identity.Establishment is not null
                && identity.EmissionPoint is not null)
            {
                estabCode = identity.Establishment.Value;
                ptoCode = identity.EmissionPoint.Value;
                sequentialOwnerId = await emitterRepository
                    .FindPreferredIdByRucAsync(identity.Ruc.Value, tenantId: null, cancellationToken)
                    .ConfigureAwait(false)
                    ?? command.EmitterId;
                await emitterRepository
                    .EnsureEstablishmentPointAsync(
                        sequentialOwnerId,
                        estabCode,
                        ptoCode,
                        DocumentTypeCode.Factura.Value,
                        identity.MainAddress,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                (estabCode, ptoCode) = await emitterRepository
                    .ResolveRegisteredFacturaPointAsync(
                        command.EmitterId,
                        command.Establishment,
                        command.EmissionPoint,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            var estab = EstablishmentCode.Create(estabCode);
            var pto = EmissionPoint.Create(ptoCode);

            var sequential = await emitterRepository
                .AllocateNextSequentialAsync(
                    sequentialOwnerId,
                    estab.Value,
                    pto.Value,
                    DocumentTypeCode.Factura.Value,
                    requestedSequential: null,
                    cancellationToken)
                .ConfigureAwait(false);

            var lines = command.Lines.Select(x =>
                new InvoiceLine(
                    x.LineNumber,
                    x.Description,
                    x.Quantity,
                    new Money(x.UnitPrice),
                    new Money(x.Discount),
                    new Money(x.LineTotalWithoutTax),
                    x.Taxes.Select(t =>
                        new LineTax(t.TaxCode, t.RateCode, t.Rate, new Money(t.TaxableBase), new Money(t.Value)))
                    .ToList(),
                    x.MainCode,
                    x.CatalogItemId,
                    x.ItemKind))
                .ToList();

            var invoice = ElectronicInvoice.Create(
                identity.Ruc,
                estab,
                pto,
                sequential,
                command.IssueDate,
                Counterparty.Create(
                    command.Counterparty.IdentificationType,
                    command.Counterparty.Identification,
                    command.Counterparty.BusinessName,
                    command.Counterparty.Address,
                    command.Counterparty.Email,
                    command.Counterparty.Phone),
                lines,
                taxRateRepository,
                command.PaymentFormCode,
                command.AdditionalNote,
                command.PaymentTermDays);

            await invoiceRepository
                .SaveNewAsync(
                    invoice,
                    command.EmitterId,
                    command.TenantId,
                    command.CreatedByUserId,
                    cancellationToken)
                .ConfigureAwait(false);

            return new CreateInvoiceSuccess(invoice);
        }
        catch (ArgumentException ex)
        {
            return new CreateInvoiceBadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new CreateInvoiceConflict(ex.Message);
        }
    }
}
