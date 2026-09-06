namespace Ecunexo.Billing.Core;


public sealed record ClaveAccesoComponents(
    DateOnly FechaEmision,
    DocumentTypeCode TipoComprobante,
    Ruc Ruc,
    string Ambiente,
    EstablishmentCode Estab,
    EmissionPoint PtoEmi,
    SequentialNumber Secuencial,
    int CodigoNumerico,
    string TipoEmision
);