using Behsazan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Behsazan.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        BaseEntityConfiguration.Configure(builder);

        #region Properties
        builder.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.NameFa)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Module)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        #endregion

        #region Indexes
        builder.HasIndex(e => e.Key)
            .IsUnique();

        builder.HasIndex(e => new { e.Module, e.Action });
        #endregion

        #region Relationships
        builder.HasMany(e => e.RolePermissions)
            .WithOne(e => e.Permission)
            .HasForeignKey(e => e.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
        #endregion
    }
}
