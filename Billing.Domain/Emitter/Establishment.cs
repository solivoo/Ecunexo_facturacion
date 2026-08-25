using Ecunexo.Billing.Domain;

namespace Ecunexo.Billing.Domain.Emitter;

public class Establishment
{
    private readonly List<EmissionPointConfig> _points = [];

    public Guid Id { get; private set; }
    public EstablishmentCode Code { get; private set; } = null!;
    public string Address { get; private set; } = string.Empty;
    public IReadOnlyList<EmissionPointConfig> Points => _points;

    private Establishment() { }

    public static Establishment Create(EstablishmentCode code, string address)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("La dirección del establecimiento es obligatoria.");

        return new Establishment
        {
            Id = Guid.NewGuid(),
            Code = code,
            Address = address
        };
    }

    public static Establishment Rehydrate(
        Guid id,
        EstablishmentCode code,
        string address,
        IEnumerable<EmissionPointConfig> points)
    {
        var est = new Establishment
        {
            Id = id,
            Code = code,
            Address = address,
        };
        foreach (var p in points)
            est._points.Add(p);
        return est;
    }

    public void AddEmissionPointConfig(EmissionPointConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (_points.Any(p => p.Code == config.Code && p.DocumentType == config.DocumentType))
            throw new InvalidOperationException("Ya existe configuración para ese punto y tipo de documento.");

        _points.Add(config);
    }

    public EmissionPointConfig GetConfig(EmissionPoint point, DocumentTypeCode documentType)
    {
        ArgumentNullException.ThrowIfNull(point);
        ArgumentNullException.ThrowIfNull(documentType);

        var config = _points.FirstOrDefault(p => p.Code == point && p.DocumentType == documentType);
        if (config is null)
            throw new InvalidOperationException("No existe configuración para el punto y tipo de documento.");

        return config;
    }
}
