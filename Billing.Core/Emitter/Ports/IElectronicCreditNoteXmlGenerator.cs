using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents;
using Ecunexo.Billing.Core.Emitter;

namespace Ecunexo.Billing.Core.Emitter.Ports;

public interface IElectronicCreditNoteXmlGenerator
{
    byte[] BuildXml(
        ElectronicCreditNote creditNote,
        InvoiceXmlEmitterContext emitter,
        ClaveAcceso accessKey);
}
