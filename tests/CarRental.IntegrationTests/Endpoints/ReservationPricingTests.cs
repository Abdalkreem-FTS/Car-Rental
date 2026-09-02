using CarRental.IntegrationTests.Support.Api;
using CarRental.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static CarRental.IntegrationTests.Support.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class ReservationPricingTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Reservation_WhenRead_ReportsTheRateItWasBookedAt()
    {
        var customer = Registration();
        await SignUpAsync(customer);

        var car = await FindCarAsync("Elantra");
        var booked = (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        await RaiseTheRateAsync(car.Id, car.DailyRate + 25m);

        await SignInAsync(customer.Email, Password);

        var afterTheRise = (await Api.Reservations.GetAsync(booked.Id)).ShouldBeOk();

        afterTheRise.DailyRate.ShouldBe(car.DailyRate, "the agreed rate is a term of the booking, not a lookup");
        afterTheRise.TotalPrice.ShouldBe(booked.TotalPrice);
        (afterTheRise.DailyRate * afterTheRise.TotalDays).ShouldBe(afterTheRise.TotalPrice,
            "the two price fields in one payload must not contradict each other");
    }

    [Fact]
    public async Task UpdateReservation_AfterTheRateChanged_ChargesTheAgreedRate()
    {
        var customer = Registration();
        await SignUpAsync(customer);

        var car = await FindCarAsync("Accent");
        var booked = (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        await RaiseTheRateAsync(car.Id, car.DailyRate * 3);

        await SignInAsync(customer.Email, Password);

        var moved = (await Api.Reservations.UpdateAsync(booked, BookingChange(20, 22))).ShouldBeOk();

        moved.DailyRate.ShouldBe(car.DailyRate, "moving dates must not silently reprice the contract");
        moved.TotalPrice.ShouldBe(booked.TotalPrice, "same number of days at the same rate");
    }

    [Fact]
    public async Task UpdateReservation_WhenTheBillChanges_ReportsWhatItWasBefore()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Clio");
        var booked = (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        var moved = (await Api.Reservations.UpdateAsync(booked, BookingChange(20, 25))).ShouldBeOk();

        moved.PreviousTotalPrice.ShouldBe(booked.TotalPrice);
        moved.TotalPrice.ShouldNotBe(booked.TotalPrice, "six days instead of three");
    }

    [Fact]
    public async Task Reservation_WhenCreated_DoesNotCarryAPreviousTotal()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Clio");

        (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated()
            .PreviousTotalPrice.ShouldBeNull("nothing was repriced");
    }

    private Task RaiseTheRateAsync(Guid carId, decimal rate) => Factory.WithDbAsync(async db =>
    {
        var car = await db.Cars.IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == carId);
        car.DailyRate = rate;

        return await db.SaveChangesAsync();
    });
}
