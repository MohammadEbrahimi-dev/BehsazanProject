using Behsazan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Behsazan.Infrastructure.Persistence.Configurations;

public class DepositConfiguration : IEntityTypeConfiguration<Deposit>
{
    public void Configure(EntityTypeBuilder<Deposit> builder)
    {
        BaseEntityConfiguration.Configure(builder);

        #region Properties
        builder.Property(e => e.ProjectId)
            .IsRequired();

        builder.Property(e => e.DepositDate)
            .IsRequired();

        builder.Property(e => e.FromAccountNo)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ToAccountNo)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(e => e.Description)
            .IsRequired(false)
            .HasMaxLength(250);

        builder.Property(e => e.TrackingNumber)
            .IsRequired(false)
            .HasMaxLength(50);

        builder.Property(e => e.ReferenceNumber)
            .IsRequired(false)
            .HasMaxLength(50);
        #endregion

        #region Indexes
        builder.HasIndex(e => e.ProjectId);
        builder.HasIndex(e => e.DepositDate);
        #endregion

        #region Relationships
        builder.HasOne(e => e.Project)
            .WithMany(e => e.Deposits)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        #endregion
    }
}
