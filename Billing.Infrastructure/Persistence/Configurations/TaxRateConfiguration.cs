using Ecunexo.Billing.Core.TaxCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecunexo.Billing.Infrastructure.Persistence.Configurations;

internal sealed class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        builder.ToTable("TaxRates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TaxCode).HasMaxLength(4).IsRequired();
        builder.Property(x => x.RateCode).HasMaxLength(4).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Rate).HasPrecision(9, 4).IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnType("date").IsRequired();
        builder.Property(x => x.ValidTo).HasColumnType("date");

        builder.HasIndex(x => new { x.TaxCode, x.RateCode, x.ValidFrom });
    }
}
