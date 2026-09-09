using Ecunexo.Billing.Api.Menu;
using Ecunexo.Billing.Api.Ride;
using Ecunexo.Billing.Business;
using Ecunexo.Billing.Core.Emitter.Ports;
using Ecunexo.Billing.Infrastructure;
using Ecunexo.Billing.Infrastructure.Persistence;
using Ecunexo.Billing.Infrastructure.Ride;
using Ecunexo.Billing.Infrastructure.Signing;
using Ecunexo.Billing.Infrastructure.Xml;
using Scalar.AspNetCore;
using Yamgooo.SRI.Sign.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);
// Persistencia RIDE (volumen /data en Docker; Data/ en local).
builder.Configuration.AddJsonFile("/data/ride-provider.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile(
    Path.Combine("Data", "ride-provider.json"),
    optional: true,
    reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new()
        {
            Title = "EcuNexo Facturación API",
            Version = "v1.0.0",
            Description =
                "Facturación electrónica SRI Ecuador. " +
                "API consumida por la SPA Cliente (HTTP); BD propia `billing`."
        };
        return Task.CompletedTask;
    });
});

var corsOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "http://localhost:5173",
    "http://localhost:5174",
    "http://localhost:5175",
    "http://127.0.0.1:5173",
    "http://127.0.0.1:5174",
    "http://127.0.0.1:5175"
};
foreach (var origin in (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
             .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
{
    corsOrigins.Add(origin);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AdminSpa",
        policy =>
            policy
                .WithOrigins(corsOrigins.ToArray())
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
});

// CORS también en respuestas de error (si no, el browser solo ve "CORS Missing Allow Origin").
builder.Services.AddProblemDetails();

var defaultConnection = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("No se encontró ConnectionStrings:Default.");

builder.Services.Configure<BillingDatabaseBootstrapOptions>(
    builder.Configuration.GetSection(BillingDatabaseBootstrapOptions.SectionName));
builder.Services.AddBillingInfrastructure(defaultConnection, builder.Configuration);
builder.Services.AddBillingBusiness();
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

await BillingDatabaseBootstrap.ApplyAsync(app.Services);

var enableApiDocs = app.Environment.IsDevelopment()
    || app.Configuration.GetValue("OpenApi:Enabled", false);

if (enableApiDocs)
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("EcuNexo Facturación")
            .WithTheme(ScalarTheme.BluePlanet);
    });
}

app.UseCors("AdminSpa");
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
