using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TotalPrice).HasPrecision(10, 2);
        builder.Property(x => x.PickupLocation).HasMaxLength(120);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.Ignore(x => x.TotalDays);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Reservations)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Car)
            .WithMany(x => x.Reservations)
            .HasForeignKey(x => x.CarId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(x => new { x.CarId, x.Status, x.StartDate, x.EndDate });
        builder.HasIndex(x => x.UserId);
    }
}
