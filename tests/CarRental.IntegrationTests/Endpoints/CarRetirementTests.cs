using CarRental.Domain.Enums;
using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Support.Api;
using CarRental.IntegrationTests.Support;
using Shouldly;
using static CarRental.IntegrationTests.Support.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class CarRetirementTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Retire_WithNoBookings_TakesTheCarOutOfTheFleet()
    {
        await SignInAsAdminAsync();
        var car = await FindCarAsync("Golf");

        var retired = (await Api.Cars.RetireAsync(car.Id)).ShouldBeOk();

        retired.Car.IsActive.ShouldBeFalse();
        retired.CancelledReservations.ShouldBe(0);
    }

    [Fact]
    public async Task Retire_WhileConfirmedBookingsRemain_IsRefusedAndChangesNothing()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Octavia");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 20, 24))).ShouldBeCreated();

        await SignInAsAdminAsync();

        (await Api.Cars.RetireAsync(car.Id)).ShouldBeConflict(CarErrors.HasActiveBookings(1));

        (await Api.Cars.GetAsync(car.Id)).ShouldBeOk().IsActive
            .ShouldBeTrue("a refused retirement must leave the car alone");
    }

    [Fact]
    public async Task Retire_WhenTheAdminAcceptsTheCancellations_ReportsHowManyItEnded()
    {
        var customer = Registration();
        await SignUpAsync(customer);

        var car = await FindCarAsync("Carnival");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 30, 32))).ShouldBeCreated();
        (await Api.Reservations.CreateAsync(Booking(car.Id, 40, 42))).ShouldBeCreated();

        await SignInAsAdminAsync();

        var retired = (await Api.Cars.RetireAsync(car.Id, cancelActiveBookings: true)).ShouldBeOk();

        retired.CancelledReservations.ShouldBe(2);
        retired.Car.IsActive.ShouldBeFalse();

        await SignInAsync(customer.Email, Password);

        (await Api.Reservations.ListAsync()).ShouldBeOk().Items
            .ShouldAllBe(reservation => reservation.Status == ReservationStatus.Cancelled);
    }

    [Fact]
    public async Task Retire_WhenTheOnlyBookingIsAlreadyCancelled_IsNotRefused()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Ranger");
        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 50, 52))).ShouldBeCreated();
        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeNoContent();

        await SignInAsAdminAsync();

        (await Api.Cars.RetireAsync(car.Id)).ShouldBeOk().CancelledReservations.ShouldBe(0);
    }

    [Fact]
    public async Task Reservations_ForACar_LetTheAdminSeeWhatRetiringWouldBreak()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Wrangler");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 60, 62))).ShouldBeCreated();

        await SignInAsAdminAsync();

        var reservations = (await Api.Cars.ReservationsAsync(car.Id)).ShouldBeOk();

        reservations.ShouldHaveSingleItem().CarMake.ShouldBe(car.Make);
    }

    [Fact]
    public async Task Reservations_ForACar_AreAdminOnly()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Wrangler");

        (await Api.Cars.ReservationsAsync(car.Id)).ShouldBeForbidden();
    }

    [Fact]
    public async Task Reservations_ForACarThatDoesNotExist_ReportsNotFound()
    {
        await SignInAsAdminAsync();

        (await Api.Cars.ReservationsAsync(Guid.NewGuid())).ShouldBeNotFound(CarErrors.NotFound);
    }
}
