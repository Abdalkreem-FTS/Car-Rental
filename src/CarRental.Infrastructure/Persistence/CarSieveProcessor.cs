using CarRental.Domain.Entities;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;

namespace CarRental.Infrastructure.Persistence;

/// <summary>
/// The allow-list of car properties a client may filter and sort on. Anything absent here is
/// rejected rather than ignored, so a typo comes back as a 400 instead of silently widening the
/// result set. Plate number, image URL, description and the active flag are deliberately left out:
/// the first two are not something to search a fleet by, and the last is ours to decide, not the
/// caller's.
/// </summary>
public sealed class CarSieveProcessor(IOptions<SieveOptions> options) : SieveProcessor(options)
{
    protected override SievePropertyMapper MapProperties(SievePropertyMapper mapper)
    {
        mapper.Property<Car>(car => car.Make).CanFilter().CanSort().HasName("make");
        mapper.Property<Car>(car => car.Model).CanFilter().CanSort().HasName("model");
        mapper.Property<Car>(car => car.Year).CanFilter().CanSort().HasName("year");
        mapper.Property<Car>(car => car.Location).CanFilter().CanSort().HasName("location");
        mapper.Property<Car>(car => car.DailyRate).CanFilter().CanSort().HasName("dailyRate");
        mapper.Property<Car>(car => car.Seats).CanFilter().CanSort().HasName("seats");
        mapper.Property<Car>(car => car.Category).CanFilter().CanSort().HasName("category");
        mapper.Property<Car>(car => car.Transmission).CanFilter().CanSort().HasName("transmission");
        mapper.Property<Car>(car => car.Fuel).CanFilter().CanSort().HasName("fuel");

        return mapper;
    }
}
