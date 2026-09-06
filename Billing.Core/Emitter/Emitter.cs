using Ecunexo.Billing.Core;

namespace Ecunexo.Billing.Core.Emitter;

public class Emitter
{
    private readonly List<Establishment> _establishments = [];

    public Guid Id { get; private set; }
    public Ruc Ruc { get; private set; } = null!;
    public string BusinessName { get; private set; } = string.Empty;
    public string? TradeName { get; private set; }
    public string MainAddress { get; private set; } = string.Empty;
    public bool Active { get; private set; }
    public IReadOnlyList<Establishment> Establishments => _establishments;
    public SigningCertificate? Certificate { get; private set; }

    private Emitter() { }

    public static Emitter Create(Ruc ruc, string businessName, string mainAddress, string? tradeName = null)
    {
        ArgumentNullException.ThrowIfNull(ruc);

        if (string.IsNullOrWhiteSpace(businessName))
            throw new ArgumentException("La razón social es obligatoria.");

        if (string.IsNullOrWhiteSpace(mainAddress))
            throw new ArgumentException("La dirección matriz es obligatoria.");

        return new Emitter
        {
            Id = Guid.NewGuid(),
            Ruc = ruc,
            BusinessName = businessName,
            TradeName = tradeName,
            MainAddress = mainAddress,
            Active = true
        };
    }

    public static Emitter Rehydrate(
        Guid id,
        Ruc ruc,
        string businessName,
        string mainAddress,
        string? tradeName,
        bool active,
        IEnumerable<Establishment> establishments,
        SigningCertificate? certificate)
    {
        var emitter = new Emitter
        {
            Id = id,
            Ruc = ruc,
            BusinessName = businessName,
            TradeName = tradeName,
            MainAddress = mainAddress,
            Active = active,
            Certificate = certificate,
        };
        foreach (var est in establishments)
            emitter._establishments.Add(est);
        return emitter;
    }

    public void AddEstablishment(Establishment establishment)
    {
        ArgumentNullException.ThrowIfNull(establishment);

        if (_establishments.Any(e => e.Code == establishment.Code))
            throw new InvalidOperationException("El establecimiento ya existe en el emisor.");

        _establishments.Add(establishment);
    }

    public void Deactivate() => Active = false;

    public void AssignCertificate(SigningCertificate certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        certificate.EnsureValidFor(Ruc);
        Certificate = certificate;
    }

    public SigningCertificate GetCertificate()
    {
        if (Certificate is null)
            throw new InvalidOperationException("El emisor no tiene certificado configurado.");

        return Certificate;
    }

    public SequentialNumber GetNextSequential(
        EstablishmentCode establishmentCode,
        EmissionPoint emissionPoint,
        DocumentTypeCode documentType)
    {
        ArgumentNullException.ThrowIfNull(establishmentCode);
        ArgumentNullException.ThrowIfNull(emissionPoint);
        ArgumentNullException.ThrowIfNull(documentType);

        var establishment = _establishments.FirstOrDefault(e => e.Code == establishmentCode);
        if (establishment is null)
            throw new InvalidOperationException("El establecimiento no existe para el emisor.");

        var config = establishment.GetConfig(emissionPoint, documentType);
        return config.NextSequential();
    }
}
