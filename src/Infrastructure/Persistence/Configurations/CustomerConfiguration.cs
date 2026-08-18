using Behsazan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Behsazan.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        BaseEntityConfiguration.Configure(builder);

        #region Properties
        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.NationalCode)
            .IsRequired(false)
            .HasMaxLength(10);

        builder.Ignore(e => e.FullName);
        #endregion

        #region Indexes
        builder.HasIndex(e => e.NationalCode)
            .IsUnique()
            .HasFilter("[NationalCode] IS NOT NULL");
        #endregion

        #region Relationships
        builder.HasMany(e => e.PhoneNumbers)
            .WithOne(e => e.Customer)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Projects)
            .WithOne(e => e.Customer)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
            .WithOne(e => e.Customer)
            .HasForeignKey<User>(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        #endregion
    }
}
