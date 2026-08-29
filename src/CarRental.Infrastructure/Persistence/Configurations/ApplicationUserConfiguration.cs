using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AddressLine1).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AddressLine2).HasMaxLength(200);
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DriverLicenseNumber).HasMaxLength(30).IsRequired();

        builder.Ignore(x => x.FullName);

        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("now()").ValueGeneratedOnAdd();

        builder.HasIndex(x => x.DriverLicenseNumber).IsUnique();

        builder.HasIndex(x => x.NormalizedEmail).IsUnique();
    }
}
