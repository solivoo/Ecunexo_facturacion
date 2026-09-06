using Ecunexo.Billing.Core.TaxCatalog;
using Ecunexo.Billing.Core.TaxRules;
using Ecunexo.Billing.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecunexo.Billing.Infrastructure.Persistence;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<WithholdingRate> WithholdingRates => Set<WithholdingRate>();
    public DbSet<TaxRule> TaxRules => Set<TaxRule>();
    public DbSet<EmitterEntity> Emitters => Set<EmitterEntity>();
    public DbSet<EstablishmentEntity> Establishments => Set<EstablishmentEntity>();
    public DbSet<EmissionPointConfigEntity> EmissionPointConfigs => Set<EmissionPointConfigEntity>();
    public DbSet<ElectronicInvoiceEntity> Invoices => Set<ElectronicInvoiceEntity>();
    public DbSet<InvoiceLineEntity> InvoiceLines => Set<InvoiceLineEntity>();
    public DbSet<InvoiceXmlArtifactEntity> InvoiceXmlArtifacts => Set<InvoiceXmlArtifactEntity>();
    public DbSet<SriOutboxEntity> SriOutbox => Set<SriOutboxEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(BillingPersistence.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
