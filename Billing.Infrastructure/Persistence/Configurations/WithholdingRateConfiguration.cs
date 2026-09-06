using Ecunexo.Billing.Core.TaxCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecunexo.Billing.Infrastructure.Persistence.Configurations;

internal sealed class WithholdingRateConfiguration : IEntityTypeConfiguration<WithholdingRate>
{
    public void Configure(EntityTypeBuilder<WithholdingRate> builder)
    {
        builder.ToTable("WithholdingRates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TaxType).HasMaxLength(4).IsRequired();
        builder.Property(x => x.RetentionCode).HasMaxLength(6).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Percentage).HasPrecision(9, 4).IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnType("date").IsRequired();
        builder.Property(x => x.ValidTo).HasColumnType("date");

        builder.HasIndex(x => new { x.TaxType, x.RetentionCode, x.ValidFrom });
    }
}
