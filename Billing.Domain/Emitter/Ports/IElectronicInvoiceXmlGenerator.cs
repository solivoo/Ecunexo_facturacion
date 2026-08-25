using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents;
using Ecunexo.Billing.Domain.Emitter;

namespace Ecunexo.Billing.Domain.Emitter.Ports;

public interface IElectronicInvoiceXmlGenerator
{
    byte[] BuildXml(
        ElectronicInvoice invoice,
        InvoiceXmlEmitterContext emitter,
        ClaveAcceso accessKey);
}
