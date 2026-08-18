using Behsazan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Behsazan.Infrastructure.Persistence.Configurations;

public class CustomerPhoneNumberConfiguration : IEntityTypeConfiguration<CustomerPhoneNumber>
{
    public void Configure(EntityTypeBuilder<CustomerPhoneNumber> builder)
    {
        BaseEntityConfiguration.Configure(builder);

        #region Properties
        builder.Property(e => e.CustomerId)
            .IsRequired();

        builder.Property(e => e.PhoneNumber)
            .IsRequired()
            .HasMaxLength(11);

        builder.Property(e => e.PhoneType)
            .IsRequired()
            .HasConversion<short>();

        builder.Property(e => e.IsBaseNumber)
            .IsRequired()
            .HasDefaultValue(false);
        #endregion

        #region Indexes
        builder.HasIndex(e => e.CustomerId);
        #endregion

        #region Relationships
        builder.HasOne(e => e.Customer)
            .WithMany(e => e.PhoneNumbers)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
        #endregion
    }
}
