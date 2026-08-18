using Behsazan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Behsazan.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        BaseEntityConfiguration.Configure(builder);

        #region Properties
        builder.Property(e => e.RoleId)
            .IsRequired();

        builder.Property(e => e.PermissionId)
            .IsRequired();
        #endregion

        #region Indexes
        builder.HasIndex(e => new { e.RoleId, e.PermissionId })
            .IsUnique();
        #endregion

        #region Relationships
        builder.HasOne(e => e.Role)
            .WithMany(e => e.RolePermissions)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Permission)
            .WithMany(e => e.RolePermissions)
            .HasForeignKey(e => e.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
        #endregion
    }
}
