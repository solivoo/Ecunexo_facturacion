namespace Ecunexo.Billing.Core.Documents;

/// <summary>
/// Quién consulta comprobantes y si puede ver los de toda la empresa.
/// </summary>
public sealed record InvoiceViewerScope(Guid? UserId, bool CanReadAll)
{
    /// <summary>
    /// Filtro de listado. <c>null</c> = sin recorte (read.all o caller sin identidad).
    /// </summary>
    public Guid? ListCreatedByFilter => CanReadAll || UserId is null ? null : UserId;

    public bool CanAccess(Guid? createdByUserId) =>
        CanReadAll || UserId is null || createdByUserId == UserId;
}
