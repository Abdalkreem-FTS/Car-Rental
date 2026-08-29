using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Shouldly;
using static CarRental.IntegrationTests.Infrastructure.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class RentalStartBoundaryTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Cancel_TheDayBeforeItStarts_IsAllowed()
    {
        var reservation = await BookStartingInAsync(3, "Corolla");

        Factory.Clock.Advance(TimeSpan.FromDays(2));

        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeNoContent();
    }

    [Fact]
    public async Task Cancel_OnTheDayItStarts_IsRefused()
    {
        var reservation = await BookStartingInAsync(3, "Picanto");

        Factory.Clock.Advance(TimeSpan.FromDays(3));

        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeConflict(ReservationErrors.AlreadyStarted);
    }

    [Fact]
    public async Task Update_OnTheDayItStarts_IsRefused()
    {
        var reservation = await BookStartingInAsync(3, "Kicks");

        Factory.Clock.Advance(TimeSpan.FromDays(3));

        (await Api.Reservations.UpdateAsync(reservation, BookingChange(20, 22)))
            .ShouldBeConflict(ReservationErrors.AlreadyStarted);
    }

    [Fact]
    public async Task Cancel_LongAfterItEnded_IsStillRefused()
    {
        var reservation = await BookStartingInAsync(3, "Elantra");

        Factory.Clock.Advance(TimeSpan.FromDays(400));

        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeConflict(ReservationErrors.AlreadyStarted);
    }

    private async Task<Application.Contracts.Reservations.ReservationResponse> BookStartingInAsync(int days, string model)
    {
        await SignUpAsync();
        var car = await FindCarAsync(model);

        return (await Api.Reservations.CreateAsync(Booking(car.Id, days, days + 2))).ShouldBeCreated();
    }
}
