using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents;
using Ecunexo.Billing.Core.Emitter;

namespace Ecunexo.Billing.Core.Emitter.Ports;

public interface IElectronicInvoiceXmlGenerator
{
    byte[] BuildXml(
        ElectronicInvoice invoice,
        InvoiceXmlEmitterContext emitter,
        ClaveAcceso accessKey);
}
