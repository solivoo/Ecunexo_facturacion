using Ecunexo.Billing.Core;

namespace Ecunexo.Billing.Core.Emitter;

public class EmissionPointConfig
{
    public Guid Id { get; private set; }
    public EmissionPoint Code { get; private set; } = null!;
    public long LastSequential { get; private set; }
    public DocumentTypeCode DocumentType { get; private set; } = null!;

    private EmissionPointConfig() { }

    public static EmissionPointConfig Create(
        EmissionPoint code,
        DocumentTypeCode documentType,
        long lastSequential = 0)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(documentType);

        if (lastSequential < 0 || lastSequential > 999_999_999)
            throw new ArgumentOutOfRangeException(nameof(lastSequential), "Secuencial fuera de rango.");

        return new EmissionPointConfig
        {
            Id = Guid.NewGuid(),
            Code = code,
            DocumentType = documentType,
            LastSequential = lastSequential
        };
    }

    public static EmissionPointConfig Rehydrate(
        Guid id,
        EmissionPoint code,
        DocumentTypeCode documentType,
        long lastSequential) =>
        new()
        {
            Id = id,
            Code = code,
            DocumentType = documentType,
            LastSequential = lastSequential,
        };

    public SequentialNumber NextSequential()
    {
        if (LastSequential >= 999_999_999)
            throw new InvalidOperationException("No hay más secuenciales disponibles.");

        LastSequential++;
        return SequentialNumber.Create(LastSequential.ToString("D9"));
    }
}
