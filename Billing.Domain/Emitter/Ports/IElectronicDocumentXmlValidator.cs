namespace Ecunexo.Billing.Domain.Emitter.Ports;

public enum ElectronicDocumentSchema
{
    FacturaV110 = 1,
    NotaCreditoV110 = 2,
}

public sealed record XmlValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static XmlValidationResult Ok() => new(true, []);

    public static XmlValidationResult Fail(IEnumerable<string> errors) =>
        new(false, errors.ToList());
}

public interface IElectronicDocumentXmlValidator
{
    XmlValidationResult Validate(byte[] xml, ElectronicDocumentSchema schema);
}
