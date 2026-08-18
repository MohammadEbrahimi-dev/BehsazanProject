using Behsazan.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Behsazan.Infrastructure.Persistence.Configurations;

public static class BaseEntityConfiguration
{
    public static void Configure<T>(EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn();

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.CreatedBy)
            .IsRequired();

        builder.Property(e => e.ModifiedAt)
            .IsRequired(false);

        builder.Property(e => e.ModifiedBy)
            .IsRequired(false);

        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .IsRequired(false);

        builder.Property(e => e.DeletedBy)
            .IsRequired(false);

        builder.HasIndex(e => e.IsDeleted);
    }
}
