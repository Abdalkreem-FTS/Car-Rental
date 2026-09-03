using CarRental.IntegrationTests.Support;
using CarRental.IntegrationTests.Support.Api;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static CarRental.IntegrationTests.Support.TestData;

namespace CarRental.IntegrationTests.Database;

public sealed class CreatedAtStampTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreatedAtUtc_ComesFromTheDatabase_NotWhicheverMachineBuiltTheObject()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");

        Factory.Clock.Advance(TimeSpan.FromDays(3650));

        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        var stamped = await Factory.WithDbAsync(db => db.Reservations
            .AsNoTracking()
            .Where(row => row.Id == reservation.Id)
            .Select(row => row.CreatedAtUtc)
            .SingleAsync());

        stamped.ShouldBeInRange(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddMinutes(5),
            "the database stamps it, so a skewed application clock cannot move it");
    }

    [Fact]
    public async Task CreatedAtUtc_ForAUser_IsStampedOnInsert()
    {
        var auth = await SignUpAsync();

        var profile = (await Api.Profile.GetAsync()).ShouldBeOk();

        profile.CreatedAtUtc.ShouldBeInRange(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddMinutes(5));

        auth.User.Id.ShouldNotBe(Guid.Empty);
    }
}
