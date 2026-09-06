using Ecunexo.Billing.Business.Invoices;
using Microsoft.Extensions.DependencyInjection;

namespace Ecunexo.Billing.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingBusiness(this IServiceCollection services)
    {
        services.AddScoped<ICreateInvoiceService, CreateInvoiceService>();
        return services;
    }
}
