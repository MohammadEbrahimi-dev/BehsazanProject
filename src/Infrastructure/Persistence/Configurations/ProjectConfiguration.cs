using Behsazan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Behsazan.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        BaseEntityConfiguration.Configure(builder);

        #region Properties
        builder.Property(e => e.CustomerId)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.GeneralLedgerNumber)
            .IsRequired(false);

        builder.Property(e => e.JoistType)
            .IsRequired()
            .HasConversion<short>();
        #endregion

        #region Indexes
        builder.HasIndex(e => e.CustomerId);
        #endregion

        #region Relationships
        builder.HasOne(e => e.Customer)
            .WithMany(e => e.Projects)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Invoices)
            .WithOne(e => e.Project)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Deposits)
            .WithOne(e => e.Project)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        #endregion
    }
}
