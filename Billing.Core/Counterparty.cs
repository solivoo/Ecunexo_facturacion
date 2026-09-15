namespace Ecunexo.Billing.Core;

public sealed record Counterparty
{
    public string IdentificationType { get; }
    public string Identification { get; }
    public string BusinessName { get; }
    public string? Address { get; }
    public string? Email { get; }
    public string? Phone { get; }

    private Counterparty(
        string tipoId,
        string id,
        string razon,
        string? dir,
        string? email,
        string? phone)
    {
        IdentificationType = tipoId;
        Identification = id;
        BusinessName = razon;
        Address = dir;
        Email = email;
        Phone = phone;
    }

    public static Counterparty Create(
        string tipoId,
        string id,
        string razon,
        string? dir = null,
        string? email = null,
        string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(tipoId))
            throw new ArgumentException("Tipo de identificación obligatorio", nameof(tipoId));
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Identificación obligatoria", nameof(id));
        if (string.IsNullOrWhiteSpace(razon))
            throw new ArgumentException("Razón social obligatoria", nameof(razon));

        var normalizedTipoId = tipoId.Trim();
        var normalizedId = id.Trim();
        var normalizedRazon = razon.Trim();

        // Catálogo SRI: 04=RUC, 05=Cédula, 06=Pasaporte, 07=Consumidor Final, 08=Identificación del Exterior
        if (normalizedTipoId is not ("04" or "05" or "06" or "07" or "08"))
            throw new ArgumentException($"Tipo de identificación '{normalizedTipoId}' no es válido según catálogo SRI (04=RUC, 05=Cédula, 06=Pasaporte, 07=Consumidor Final, 08=Exterior).", nameof(tipoId));

        if (normalizedTipoId == "04")
        {
            if (normalizedId.Length != 13 || !normalizedId.All(char.IsAsciiDigit))
                throw new ArgumentException("El RUC del comprador debe contener exactamente 13 dígitos numéricos.", nameof(id));
        }
        else if (normalizedTipoId == "05")
        {
            if (normalizedId.Length != 10 || !normalizedId.All(char.IsAsciiDigit))
                throw new ArgumentException("La cédula del comprador debe contener exactamente 10 dígitos numéricos.", nameof(id));
        }
        else if (normalizedTipoId == "07")
        {
            normalizedId = "9999999999999";
            if (string.IsNullOrWhiteSpace(normalizedRazon) || normalizedRazon.Equals("CONSUMIDOR FINAL", StringComparison.OrdinalIgnoreCase))
                normalizedRazon = "CONSUMIDOR FINAL";
        }

        if (normalizedRazon.Length < 2)
            throw new ArgumentException("La razón social del comprador debe tener al menos 2 caracteres.", nameof(razon));
        if (normalizedRazon.Length > 300)
            throw new ArgumentException("La razón social del comprador no puede superar 300 caracteres.", nameof(razon));

        string? normalizedDir = null;
        if (!string.IsNullOrWhiteSpace(dir))
        {
            normalizedDir = dir.Trim();
            if (normalizedDir.Length > 300)
                throw new ArgumentException("La dirección del comprador no puede superar 300 caracteres.", nameof(dir));
        }

        string? normalizedEmail = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            normalizedEmail = email.Trim();
            if (normalizedEmail.Length > 320)
                throw new ArgumentException("El correo no puede superar 320 caracteres.", nameof(email));
            if (normalizedEmail.AsSpan().IndexOf('@') <= 0
                || normalizedEmail.AsSpan().LastIndexOf('@') == normalizedEmail.Length - 1)
            {
                throw new ArgumentException("El correo del comprador no es válido.", nameof(email));
            }
        }

        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            normalizedPhone = phone.Trim();
            if (normalizedPhone.Length > 30)
                throw new ArgumentException("El teléfono no puede superar 30 caracteres.", nameof(phone));
        }

        return new Counterparty(normalizedTipoId, normalizedId, normalizedRazon, normalizedDir, normalizedEmail, normalizedPhone);
    }
}