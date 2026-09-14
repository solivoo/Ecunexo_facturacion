using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Sri;

namespace Ecunexo.Billing.Core.Emitter;

public class EmissionPointConfig
{
    public Guid Id { get; private set; }
    public EmissionPoint Code { get; private set; } = null!;
    public long LastSequential { get; private set; }
    public long LastTestSequential { get; private set; }
    public DocumentTypeCode DocumentType { get; private set; } = null!;

    private EmissionPointConfig() { }

    public static EmissionPointConfig Create(
        EmissionPoint code,
        DocumentTypeCode documentType,
        long lastSequential = 0,
        long lastTestSequential = 0)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(documentType);

        if (lastSequential < 0 || lastSequential > 999_999_999)
            throw new ArgumentOutOfRangeException(nameof(lastSequential), "Secuencial fuera de rango.");
        if (lastTestSequential < 0 || lastTestSequential > 999_999_999)
            throw new ArgumentOutOfRangeException(nameof(lastTestSequential), "Secuencial de prueba fuera de rango.");

        return new EmissionPointConfig
        {
            Id = Guid.NewGuid(),
            Code = code,
            DocumentType = documentType,
            LastSequential = lastSequential,
            LastTestSequential = lastTestSequential
        };
    }

    public static EmissionPointConfig Rehydrate(
        Guid id,
        EmissionPoint code,
        DocumentTypeCode documentType,
        long lastSequential,
        long lastTestSequential = 0) =>
        new()
        {
            Id = id,
            Code = code,
            DocumentType = documentType,
            LastSequential = lastSequential,
            LastTestSequential = lastTestSequential,
        };

    public SequentialNumber NextSequential(SriEnvironment environment = SriEnvironment.Production)
    {
        if (environment == SriEnvironment.Test)
        {
            if (LastTestSequential >= 999_999_999)
                throw new InvalidOperationException("No hay más secuenciales de prueba disponibles.");

            LastTestSequential++;
            return SequentialNumber.Create(LastTestSequential.ToString("D9"));
        }

        if (LastSequential >= 999_999_999)
            throw new InvalidOperationException("No hay más secuenciales disponibles.");

        LastSequential++;
        return SequentialNumber.Create(LastSequential.ToString("D9"));
    }
}
