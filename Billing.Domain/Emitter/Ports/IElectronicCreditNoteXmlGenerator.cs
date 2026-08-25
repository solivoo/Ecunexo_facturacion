using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents;
using Ecunexo.Billing.Domain.Emitter;

namespace Ecunexo.Billing.Domain.Emitter.Ports;

public interface IElectronicCreditNoteXmlGenerator
{
    byte[] BuildXml(
        ElectronicCreditNote creditNote,
        InvoiceXmlEmitterContext emitter,
        ClaveAcceso accessKey);
}
