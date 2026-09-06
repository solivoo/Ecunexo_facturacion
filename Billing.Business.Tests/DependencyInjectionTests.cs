using Ecunexo.Billing.Business;
using Ecunexo.Billing.Business.Invoices;
using Microsoft.Extensions.DependencyInjection;

namespace Ecunexo.Billing.Business.Tests;

public sealed class DependencyInjectionTests
{
    [Fact(DisplayName = "AddBillingBusiness registra CreateInvoiceService")]
    public void AddBillingBusiness_RegistersCreateInvoiceService()
    {
        var services = new ServiceCollection();
        services.AddBillingBusiness();

        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICreateInvoiceService));

        Assert.NotNull(descriptor);
        Assert.Equal(typeof(CreateInvoiceService), descriptor.ImplementationType);
    }
}
