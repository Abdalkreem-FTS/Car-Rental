using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static CarRental.IntegrationTests.Infrastructure.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class RetiredCarBookingTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateReservation_OnACarThatLeftTheFleet_IsRefused()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Golf");

        await RetireOutsideTheApiAsync(car.Id);

        (await Api.Reservations.CreateAsync(Booking(car.Id, 20, 22))).ShouldBeNotFound(CarErrors.NotFound);
    }

    [Fact]
    public async Task UpdateReservation_OnACarThatLeftTheFleet_IsRefused()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Octavia");
        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 20, 22))).ShouldBeCreated();

        await RetireOutsideTheApiAsync(car.Id);

        var response = await Api.Reservations.UpdateAsync(
            reservation.Id,
            BookingChange(30, 32),
            reservation.Version);

        response.ShouldBeConflict(CarErrors.NoLongerInTheFleet);
    }

    [Fact]
    public async Task CancelReservation_OnACarThatLeftTheFleet_IsStillAllowed()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Kicks");
        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 20, 22))).ShouldBeCreated();

        await RetireOutsideTheApiAsync(car.Id);

        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeNoContent(
            "a customer must always be able to get out of a booking");
    }

    [Fact]
    public async Task ListReservations_OnACarThatLeftTheFleet_StillShowsIt()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Picanto");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 20, 22))).ShouldBeCreated();

        await RetireOutsideTheApiAsync(car.Id);

        (await Api.Reservations.ListAsync()).ShouldBeOk().Items.ShouldHaveSingleItem().CarMake.ShouldBe(car.Make);
    }

    private Task RetireOutsideTheApiAsync(Guid carId) => Factory.WithDbAsync(async db =>
    {
        var stored = await db.Cars.IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == carId);
        stored.IsActive = false;

        return await db.SaveChangesAsync();
    });
}
