using Ecunexo.Billing.Core.Documents;

namespace Ecunexo.Billing.Business.Invoices;

public abstract record CreateInvoiceResult;

public sealed record CreateInvoiceSuccess(ElectronicInvoice Invoice) : CreateInvoiceResult;

public sealed record CreateInvoiceNotFound(string Message) : CreateInvoiceResult;

public sealed record CreateInvoiceBadRequest(string Message) : CreateInvoiceResult;

public sealed record CreateInvoiceConflict(string Message) : CreateInvoiceResult;
