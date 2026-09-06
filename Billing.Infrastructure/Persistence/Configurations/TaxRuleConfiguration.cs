using Ecunexo.Billing.Core.TaxRules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecunexo.Billing.Infrastructure.Persistence.Configurations;

internal sealed class TaxRuleConfiguration : IEntityTypeConfiguration<TaxRule>
{
    public void Configure(EntityTypeBuilder<TaxRule> builder)
    {
        builder.ToTable("TaxRules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnType("date").IsRequired();
        builder.Property(x => x.ValidTo).HasColumnType("date");
        builder.HasIndex(x => new { x.Code, x.ValidFrom }).IsUnique();
    }
}
