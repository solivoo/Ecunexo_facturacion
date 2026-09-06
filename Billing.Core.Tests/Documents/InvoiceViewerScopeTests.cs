using Ecunexo.Billing.Core.Documents;

namespace Ecunexo.Billing.Core.Tests.Documents;

public sealed class InvoiceViewerScopeTests
{
    private static readonly Guid Seller = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Other = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact(DisplayName = "read.all ve facturas propias, ajenas y sin dueño")]
    public void CanReadAll_AccessesAnyOwner()
    {
        var scope = new InvoiceViewerScope(Seller, CanReadAll: true);

        Assert.True(scope.CanAccess(Seller));
        Assert.True(scope.CanAccess(Other));
        Assert.True(scope.CanAccess(null));
        Assert.Null(scope.ListCreatedByFilter);
    }

    [Fact(DisplayName = "Sin read.all solo ve las que emitió")]
    public void OwnScope_OnlyMatchesSelf()
    {
        var scope = new InvoiceViewerScope(Seller, CanReadAll: false);

        Assert.True(scope.CanAccess(Seller));
        Assert.False(scope.CanAccess(Other));
        Assert.False(scope.CanAccess(null));
        Assert.Equal(Seller, scope.ListCreatedByFilter);
    }

    [Fact(DisplayName = "Caller sin identidad (tests/scripts) no recorta")]
    public void OwnScope_WithoutUser_AllowsLegacyCallers()
    {
        var scope = new InvoiceViewerScope(null, CanReadAll: false);

        Assert.True(scope.CanAccess(Seller));
        Assert.True(scope.CanAccess(null));
        Assert.Null(scope.ListCreatedByFilter);
    }
}
