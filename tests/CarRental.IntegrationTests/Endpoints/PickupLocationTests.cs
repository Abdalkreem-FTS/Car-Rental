using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Support;
using CarRental.IntegrationTests.Support.Api;
using Shouldly;
using static CarRental.IntegrationTests.Support.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class PickupLocationTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateReservation_AskingToCollectACarFromAnotherCity_IsRefused()
    {
        await SignUpAsync();

        var inAqaba = await FindCarAsync("Wrangler");
        inAqaba.Location.ShouldBe("Aqaba");

        var response = await Api.Reservations.CreateAsync(
            Booking(inAqaba.Id, 5, 7) with { PickupLocation = "Amman" });

        response.ShouldFailValidationOn("pickupLocation");
    }

    [Fact]
    public async Task CreateReservation_WithNoPickupLocation_CollectsItWhereItIs()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Wrangler");

        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        reservation.PickupLocation.ShouldBe(car.Location);
    }

    [Fact]
    public async Task CreateReservation_NamingTheCarsOwnCity_IsAccepted()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Wrangler");

        var reservation = (await Api.Reservations.CreateAsync(
            Booking(car.Id, 5, 7) with { PickupLocation = "  aqaba  " })).ShouldBeCreated();

        reservation.PickupLocation.ShouldBe("Aqaba", "the stored value is the canonical spelling, not what was typed");
    }

    [Fact]
    public async Task UpdateReservation_MovingThePickupToAnotherCity_IsRefused()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Ranger");
        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        var response = await Api.Reservations.UpdateAsync(
            reservation.Id,
            BookingChange(20, 22) with { PickupLocation = "Irbid" },
            reservation.Version);

        response.ShouldFailValidationOn("pickupLocation");
    }
}
