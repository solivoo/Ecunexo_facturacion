namespace Ecunexo.Billing.Domain;

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
            throw new ArgumentException("Tipo de identificación obligatorio");
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Identificación obligatoria");
        if (string.IsNullOrWhiteSpace(razon))
            throw new ArgumentException("Razón social obligatoria");

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

        return new Counterparty(tipoId, id, razon, dir, normalizedEmail, normalizedPhone);
    }
}