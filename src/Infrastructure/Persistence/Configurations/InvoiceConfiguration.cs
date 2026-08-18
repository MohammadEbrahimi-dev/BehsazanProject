using Behsazan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Behsazan.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        BaseEntityConfiguration.Configure(builder);

        #region Properties
        builder.Property(e => e.ProjectId)
            .IsRequired();

        builder.Property(e => e.InvoiceNumber)
            .IsRequired();

        builder.Property(e => e.InvoiceDate)
            .IsRequired()
            .HasColumnType("datetime");

        builder.Property(e => e.Title)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(e => e.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 5);

        builder.Property(e => e.TotalPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(e => e.ShippingCost)
            .IsRequired(false)
            .HasPrecision(18, 2);
        #endregion

        #region Indexes
        builder.HasIndex(e => e.ProjectId);
        builder.HasIndex(e => e.InvoiceNumber)
            .IsUnique();
        #endregion

        #region Relationships
        builder.HasOne(e => e.Project)
            .WithMany(e => e.Invoices)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.InvoiceItems)
            .WithOne(e => e.Invoice)
            .HasForeignKey(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        #endregion
    }
}
