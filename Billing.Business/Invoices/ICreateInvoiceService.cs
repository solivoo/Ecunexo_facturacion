namespace Ecunexo.Billing.Business.Invoices;

public interface ICreateInvoiceService
{
    Task<CreateInvoiceResult> ExecuteAsync(CreateInvoiceCommand command, CancellationToken cancellationToken = default);
}
