using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public sealed class CarConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Make).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Model).HasMaxLength(60).IsRequired();
        builder.Property(x => x.PlateNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.DailyRate).HasPrecision(10, 2);

        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Transmission).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Fuel).HasConversion<string>().HasMaxLength(20);

        builder.HasQueryFilter(x => x.IsActive);

        builder.HasIndex(x => x.PlateNumber).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.Location });

        builder.HasIndex(x => x.Make).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(x => x.Model).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(x => x.Location).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}
