using Behsazan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Behsazan.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        BaseEntityConfiguration.Configure(builder);

        #region Properties
        builder.Property(e => e.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CustomerId)
            .IsRequired(false);
        #endregion

        #region Indexes
        builder.HasIndex(e => e.Username)
            .IsUnique();

        builder.HasIndex(e => e.CustomerId)
            .IsUnique(false)
            .HasFilter("[CustomerId] IS NOT NULL");
        #endregion

        #region Relationships
        builder.HasOne(e => e.Customer)
            .WithOne(e => e.User)
            .HasForeignKey<User>(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.UserRoles)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        #endregion
    }
}
