using Ecunexo.Billing.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecunexo.Billing.Infrastructure.Persistence.Configurations;

internal sealed class EmitterEntityConfiguration : IEntityTypeConfiguration<EmitterEntity>
{
    public void Configure(EntityTypeBuilder<EmitterEntity> builder)
    {
        builder.ToTable("emitters");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Ruc).HasMaxLength(13).IsRequired();
        builder.Property(x => x.BusinessName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.TradeName).HasMaxLength(300);
        builder.Property(x => x.MainAddress).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.Ruc);
        builder.HasIndex(x => x.TenantId);
        builder.HasMany(x => x.Establishments)
            .WithOne(x => x.Emitter)
            .HasForeignKey(x => x.EmitterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class EstablishmentEntityConfiguration : IEntityTypeConfiguration<EstablishmentEntity>
{
    public void Configure(EntityTypeBuilder<EstablishmentEntity> builder)
    {
        builder.ToTable("establishments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.EmitterId, x.Code }).IsUnique();
        builder.HasMany(x => x.Points)
            .WithOne(x => x.Establishment)
            .HasForeignKey(x => x.EstablishmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class EmissionPointConfigEntityConfiguration : IEntityTypeConfiguration<EmissionPointConfigEntity>
{
    public void Configure(EntityTypeBuilder<EmissionPointConfigEntity> builder)
    {
        builder.ToTable("emission_point_configs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmissionPoint).HasMaxLength(3).IsRequired();
        builder.Property(x => x.DocumentType).HasMaxLength(2).IsRequired();
        builder.HasIndex(x => new { x.EstablishmentId, x.EmissionPoint, x.DocumentType }).IsUnique();
    }
}

internal sealed class ElectronicInvoiceEntityConfiguration : IEntityTypeConfiguration<ElectronicInvoiceEntity>
{
    public void Configure(EntityTypeBuilder<ElectronicInvoiceEntity> builder)
    {
        builder.ToTable("electronic_invoices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmitterRuc).HasMaxLength(13).IsRequired();
        builder.Property(x => x.Establishment).HasMaxLength(3).IsRequired();
        builder.Property(x => x.EmissionPoint).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Sequential).HasMaxLength(9).IsRequired();
        builder.Property(x => x.DocumentType).HasMaxLength(2).IsRequired();
        builder.Property(x => x.AccessKey).HasMaxLength(49);
        builder.Property(x => x.State).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CounterpartyJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.PaymentFormCode).HasMaxLength(2).IsRequired().HasDefaultValue("01");
        builder.Property(x => x.AdditionalNote).HasMaxLength(300);
        builder.Property(x => x.PaymentTermDays).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ModifiedDocumentType).HasMaxLength(2);
        builder.Property(x => x.ModifiedDocumentNumber).HasMaxLength(17);
        builder.Property(x => x.Motivo).HasMaxLength(300);
        builder.Property(x => x.TaxTotalsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.SriMessagesJson).HasColumnType("jsonb");
        builder.Property(x => x.SubtotalWithoutTax).HasPrecision(18, 2);
        builder.Property(x => x.GrandTotal).HasPrecision(18, 2);
        builder.Property(x => x.SriTransmissionState).HasMaxLength(32);
        builder.HasIndex(x => new
        {
            x.EmitterId,
            x.Establishment,
            x.EmissionPoint,
            x.DocumentType,
            x.Sequential,
        }).IsUnique();
        builder.HasIndex(x => x.AccessKey);
        builder.HasIndex(x => new { x.EmitterId, x.IssueDate });
        builder.HasIndex(x => new { x.EmitterId, x.CreatedByUserId });
        builder.HasIndex(x => x.ModifiedInvoiceId);
        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.XmlArtifact)
            .WithOne(x => x.Invoice)
            .HasForeignKey<InvoiceXmlArtifactEntity>(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Emitter)
            .WithMany()
            .HasForeignKey(x => x.EmitterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InvoiceLineEntityConfiguration : IEntityTypeConfiguration<InvoiceLineEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceLineEntity> builder)
    {
        builder.ToTable("invoice_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.MainCode).HasMaxLength(25);
        builder.Property(x => x.ItemKind).HasMaxLength(16);
        builder.Property(x => x.Quantity).HasPrecision(18, 6);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 6);
        builder.Property(x => x.Discount).HasPrecision(18, 2);
        builder.Property(x => x.LineTotalWithoutTax).HasPrecision(18, 2);
        builder.Property(x => x.TaxesJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.InvoiceId, x.LineNumber }).IsUnique();
    }
}

internal sealed class InvoiceXmlArtifactEntityConfiguration : IEntityTypeConfiguration<InvoiceXmlArtifactEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceXmlArtifactEntity> builder)
    {
        builder.ToTable("invoice_xml_artifacts");
        builder.HasKey(x => x.InvoiceId);
    }
}

internal sealed class SriOutboxEntityConfiguration : IEntityTypeConfiguration<SriOutboxEntity>
{
    public void Configure(EntityTypeBuilder<SriOutboxEntity> builder)
    {
        builder.ToTable("sri_outbox");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Operation).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(16).IsRequired();
        builder.Property(x => x.AccessKey).HasMaxLength(49);
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
        builder.HasIndex(x => x.InvoiceId);
    }
}
