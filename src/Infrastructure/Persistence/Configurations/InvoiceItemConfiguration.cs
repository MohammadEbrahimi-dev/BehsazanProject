using Behsazan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Behsazan.Infrastructure.Persistence.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        BaseEntityConfiguration.Configure(builder);

        #region Properties
        builder.Property(e => e.InvoiceId)
            .IsRequired();

        builder.Property(e => e.Length)
            .IsRequired()
            .HasPrecision(8, 4);

        builder.Property(e => e.Count)
            .IsRequired();

        builder.Property(e => e.BottomRebar)
            .IsRequired();

        builder.Property(e => e.TopRebar)
            .IsRequired();

        builder.Property(e => e.ReinforcementBar)
            .IsRequired(false);

        builder.Property(e => e.ReinforcementPercent)
            .IsRequired(false);

        builder.Property(e => e.Zigzag)
            .IsRequired();

        builder.Property(e => e.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 5);
        #endregion

        #region Indexes
        builder.HasIndex(e => e.InvoiceId);
        #endregion

        #region Relationships
        builder.HasOne(e => e.Invoice)
            .WithMany(e => e.InvoiceItems)
            .HasForeignKey(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        #endregion
    }
}
