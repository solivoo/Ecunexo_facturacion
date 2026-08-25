using Ecunexo.Billing.Api.Menu;
using Ecunexo.Billing.Api.Ride;
using Ecunexo.Billing.Domain.Emitter.Ports;
using Ecunexo.Billing.Infrastructure;
using Ecunexo.Billing.Infrastructure.Ride;
using Ecunexo.Billing.Infrastructure.Signing;
using Ecunexo.Billing.Infrastructure.Xml;
using Yamgooo.SRI.Sign.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);
builder.Configuration.AddJsonFile(
    Path.Combine("Data", "ride-provider.json"),
    optional: true,
    reloadOnChange: true);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AdminDev",
        policy =>
            policy
                .WithOrigins(
                    "http://localhost:5173",
                    "http://localhost:5174",
                    "http://localhost:5175",
                    "http://127.0.0.1:5173",
                    "http://127.0.0.1:5174",
                    "http://127.0.0.1:5175")
                .AllowAnyHeader()
                .AllowAnyMethod());
});

var defaultConnection = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("No se encontró ConnectionStrings:Default.");

builder.Services.AddBillingInfrastructure(defaultConnection, builder.Configuration);
builder.Services.Configure<RideProviderOptions>(
    builder.Configuration.GetSection(RideProviderOptions.SectionName));
builder.Services.AddSingleton<RideProviderResolver>();
builder.Services.AddSingleton<RideProviderFileStore>();

builder.Services.AddSingleton<IBillingMenuProvider, BillingMenuProvider>();
builder.Services.AddSingleton<IElectronicInvoiceXmlGenerator, SriFacturaXmlGenerator>();
builder.Services.AddSingleton<IElectronicCreditNoteXmlGenerator, SriNotaCreditoXmlGenerator>();
builder.Services.AddSingleton<IElectronicDocumentXmlValidator, XsdElectronicDocumentXmlValidator>();
builder.Services.AddSriSignService();
builder.Services.AddScoped<IElectronicSignatureService, XadesElectronicSignatureService>();

var app = builder.Build();

app.UseCors("AdminDev");
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
