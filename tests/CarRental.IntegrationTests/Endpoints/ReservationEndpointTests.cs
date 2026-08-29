using CarRental.Domain.Enums;
using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static CarRental.IntegrationTests.Infrastructure.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class ReservationEndpointTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateReservation_WithADateRange_PricesItInclusivelyOnBothEnds()
    {
        await SignUpAsync();
        var car = await FindCarAsync("RAV4");

        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 7, 11))).ShouldBeCreated();

        reservation.TotalDays.ShouldBe(5);
        reservation.DailyRate.ShouldBe(car.DailyRate);
        reservation.TotalPrice.ShouldBe(car.DailyRate * 5);
        reservation.Status.ShouldBe(ReservationStatus.Confirmed);
        reservation.CarMake.ShouldBe(car.Make);
    }

    [Fact]
    public async Task CreateReservation_WithTheSamePickupAndReturnDay_BillsOneDay()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Picanto");

        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 3, 3))).ShouldBeCreated();

        reservation.TotalDays.ShouldBe(1);
        reservation.TotalPrice.ShouldBe(car.DailyRate);
    }

    [Fact]
    public async Task CreateReservation_WithoutAPickupLocation_DefaultsToWhereTheCarLives()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Wrangler");

        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 4, 5))).ShouldBeCreated();

        reservation.PickupLocation.ShouldBe(car.Location);
    }

    [Fact]
    public async Task CreateReservation_WhenItSucceeds_ReturnsTheLocationOfTheNewReservation()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");

        var response = await Api.Reservations.CreateAsync(Booking(car.Id, 2, 3));

        var reservation = response.ShouldBeCreated();
        response.Location!.ToString().ShouldBe($"/api/reservations/{reservation.Id}");
    }
    
    [Theory]
    [InlineData(10, 15, "identical")]
    [InlineData(8, 11, "overlaps the start")]
    [InlineData(14, 18, "overlaps the end")]
    [InlineData(12, 13, "sits inside")]
    [InlineData(5, 20, "swallows it")]
    [InlineData(15, 16, "touches the last day")]
    [InlineData(9, 10, "touches the first day")]
    public async Task CreateReservation_WithDatesOverlappingAConfirmedBooking_ReportsAConflict(
        int fromDay, int toDay, string _)
    {
        await SignUpAsync();
        var car = await FindCarAsync("RAV4");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 10, 15))).ShouldBeCreated();

        var response = await Api.Reservations.CreateAsync(Booking(car.Id, fromDay, toDay));

        response.ShouldBeConflict(CarErrors.Unavailable);
    }

    [Theory]
    [InlineData(5, 9, "ends the day before")]
    [InlineData(16, 20, "starts the day after")]
    public async Task CreateReservation_WithDatesThatOnlyAbutAnExistingBooking_Succeeds(
        int fromDay, int toDay, string _)
    {
        await SignUpAsync();
        var car = await FindCarAsync("RAV4");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 10, 15))).ShouldBeCreated();

        (await Api.Reservations.CreateAsync(Booking(car.Id, fromDay, toDay))).ShouldBeCreated();
    }

    [Fact]
    public async Task Search_ForDatesACarIsBookedOn_OmitsItButKeepsItForOtherDays()
    {
        await SignUpAsync();
        var car = await FindCarAsync("RAV4");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 10, 15))).ShouldBeCreated();

        var during = (await Api.Cars.SearchAsync(new CarQuery
        {
            Query = "RAV4",
            PickupDate = In(12),
            ReturnDate = In(13),
        })).ShouldBeOk();

        var after = (await Api.Cars.SearchAsync(new CarQuery
        {
            Query = "RAV4",
            PickupDate = In(20),
            ReturnDate = In(22),
        })).ShouldBeOk();

        during.TotalCount.ShouldBe(0);
        after.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task CreateReservation_AfterAnOverlappingBookingIsCancelled_Succeeds()
    {
        await SignUpAsync();
        var car = await FindCarAsync("RAV4");
        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 10, 15))).ShouldBeCreated();

        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeNoContent();

        (await Api.Reservations.CreateAsync(Booking(car.Id, 11, 14))).ShouldBeCreated();
    }

    [Fact]
    public async Task CreateReservation_ForADifferentCarOnTheSameDates_Succeeds()
    {
        await SignUpAsync();
        var first = await FindCarAsync("RAV4");
        var second = await FindCarAsync("Wrangler");
        (await Api.Reservations.CreateAsync(Booking(first.Id, 10, 15))).ShouldBeCreated();

        (await Api.Reservations.CreateAsync(Booking(second.Id, 10, 15))).ShouldBeCreated();
    }
    
    [Fact]
    public async Task CreateReservation_WithAnUnknownCar_ReportsNotFound()
    {
        await SignUpAsync();

        (await Api.Reservations.CreateAsync(Booking(Guid.NewGuid(), 2, 3))).ShouldBeNotFound(CarErrors.NotFound);
    }

    [Fact]
    public async Task ListReservations_WhenTheCarWasRetiredAfterBooking_StillRendersTheBooking()
    {
        var registration = TestData.Registration();
        await SignUpAsync(registration);

        var car = await FindCarAsync("Hiace");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 12, 14))).ShouldBeCreated();

        await SignInAsAdminAsync();
        (await Api.Cars.RetireAsync(car.Id, cancelActiveBookings: true)).ShouldBeOk()
            .CancelledReservations.ShouldBe(1);

        await SignInAsync(registration.Email, TestData.Password);

        var mine = (await Api.Reservations.ListAsync()).ShouldBeOk().Items.ShouldHaveSingleItem();

        mine.CarMake.ShouldBe(car.Make, "retiring a car must not blank out the bookings that reference it");
        mine.CarModel.ShouldBe(car.Model);
        mine.Status.ShouldBe(ReservationStatus.Cancelled);
    }

    [Fact]
    public async Task CreateReservation_WithARetiredCar_ReportsNotFound()
    {
        await SignInAsAdminAsync();
        var car = await FindCarAsync("Clio");
        (await Api.Cars.RetireAsync(car.Id)).ShouldBeOk();

        await SignUpAsync();

        (await Api.Reservations.CreateAsync(Booking(car.Id, 2, 3))).ShouldBeNotFound(CarErrors.NotFound);
    }

    [Fact]
    public async Task CreateReservation_WithAPickupDateInThePast_ReportsAValidationError()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");

        (await Api.Reservations.CreateAsync(Booking(car.Id, -3, 2))).ShouldFailValidationOn("startDate");
    }

    [Fact]
    public async Task CreateReservation_WithAReturnDateBeforeThePickupDate_ReportsAValidationError()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");

        (await Api.Reservations.CreateAsync(Booking(car.Id, 10, 4))).ShouldFailValidationOn("endDate");
    }

    [Fact]
    public async Task CreateReservation_LongerThanNinetyDays_ReportsAValidationError()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");

        (await Api.Reservations.CreateAsync(Booking(car.Id, 1, 95))).ShouldFailValidationOn("endDate");
    }

    [Fact]
    public async Task CreateReservation_WithoutAToken_ReportsUnauthorized()
    {
        SignOut();

        (await Api.Reservations.CreateAsync(Booking(Guid.NewGuid(), 2, 3))).ShouldRequireAuthentication();
    }
    
    [Fact]
    public async Task GetReservations_WhenOtherAccountsHaveBookings_ReturnsOnlyTheCallersOwn()
    {
        await SignUpAsync();
        var mine = (await Api.Reservations.CreateAsync(Booking((await FindCarAsync("RAV4")).Id, 10, 12))).ShouldBeCreated();

        await SignUpAsync();
        (await Api.Reservations.CreateAsync(Booking((await FindCarAsync("Wrangler")).Id, 10, 12))).ShouldBeCreated();

        var theirs = (await Api.Reservations.ListAsync()).ShouldBeOk();

        theirs.Items.ShouldHaveSingleItem().Id.ShouldNotBe(mine.Id);

        (await Factory.WithDbAsync(db => db.Reservations.CountAsync())).ShouldBe(2);
    }

    [Fact]
    public async Task GetReservation_ForAnotherUsersBooking_IsIndistinguishableFromOneThatDoesNotExist()
    {
        await SignUpAsync();
        var reservation = (await Api.Reservations.CreateAsync(Booking((await FindCarAsync("RAV4")).Id, 10, 12))).ShouldBeCreated();

        await SignUpAsync();

        var someoneElses = await Api.Reservations.GetAsync(reservation.Id);
        var neverExisted = await Api.Reservations.GetAsync(Guid.NewGuid());

        someoneElses.ShouldBeNotFound(ReservationErrors.NotFound);
        someoneElses.StatusCode.ShouldBe(neverExisted.StatusCode);
        someoneElses.Problem!.ErrorCode.ShouldBe(neverExisted.Problem!.ErrorCode);

        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeNotFound(ReservationErrors.NotFound);
    }

    [Fact]
    public async Task GetReservation_WithAnUnknownId_ReportsNotFound()
    {
        await SignUpAsync();

        (await Api.Reservations.GetAsync(Guid.NewGuid())).ShouldBeNotFound(ReservationErrors.NotFound);
    }

    [Fact]
    public async Task GetReservations_WhenSeveralExist_ListsTheLatestPickupFirst()
    {
        await SignUpAsync();
        var car = await FindCarAsync("RAV4");

        foreach (var (from, to) in new[] { (5, 6), (30, 31), (15, 16) })
        {
            (await Api.Reservations.CreateAsync(Booking(car.Id, from, to))).ShouldBeCreated();
        }

        var reservations = (await Api.Reservations.ListAsync()).ShouldBeOk();

        reservations.Items.Select(r => r.StartDate).ShouldBe(reservations.Items.Select(r => r.StartDate).OrderDescending());
    }
    
    [Fact]
    public async Task UpdateReservation_WithNewDates_MovesItAndRepricesTheRental()
    {
        await SignUpAsync();
        var car = await FindCarAsync("RAV4");
        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 10, 12))).ShouldBeCreated();

        var moved = (await Api.Reservations.UpdateAsync(reservation, BookingChange(20, 24))).ShouldBeOk();

        moved.Id.ShouldBe(reservation.Id);
        moved.StartDate.ShouldBe(In(20));
        moved.TotalDays.ShouldBe(5);
        moved.TotalPrice.ShouldBe(car.DailyRate * 5, "the new span is repriced at the car's rate");
        moved.TotalPrice.ShouldNotBe(reservation.TotalPrice);
    }

    [Fact]
    public async Task UpdateReservation_ToDatesOverlappingItsOwnCurrentBooking_Succeeds()
    {
        await SignUpAsync();
        var car = await FindCarAsync("RAV4");
        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 10, 15))).ShouldBeCreated();

        (await Api.Reservations.UpdateAsync(reservation, BookingChange(10, 16))).ShouldBeOk();
    }

    [Fact]
    public async Task UpdateReservation_ToDatesAnotherBookingHolds_ReportsAConflict()
    {
        await SignUpAsync();
        var car = await FindCarAsync("RAV4");
        var mine = (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();
        (await Api.Reservations.CreateAsync(Booking(car.Id, 20, 25))).ShouldBeCreated();

        var response = await Api.Reservations.UpdateAsync(mine, BookingChange(22, 23));

        response.ShouldBeConflict(CarErrors.Unavailable);
    }

    [Fact]
    public async Task UpdateReservation_FreesTheDatesItUsedToHold()
    {
        await SignUpAsync();
        var car = await FindCarAsync("RAV4");
        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 10, 15))).ShouldBeCreated();

        (await Api.Reservations.UpdateAsync(reservation, BookingChange(30, 32))).ShouldBeOk();

        (await Api.Reservations.CreateAsync(Booking(car.Id, 11, 14))).ShouldBeCreated();
    }

    [Fact]
    public async Task UpdateReservation_ForAnotherUsersBooking_ReportsForbidden()
    {
        await SignUpAsync();
        var reservation = (await Api.Reservations.CreateAsync(Booking((await FindCarAsync("RAV4")).Id, 10, 12))).ShouldBeCreated();

        await SignUpAsync();

        (await Api.Reservations.UpdateAsync(reservation, BookingChange(20, 22)))
            .ShouldBeNotFound(ReservationErrors.NotFound);
    }

    [Fact]
    public async Task UpdateReservation_WhenAlreadyCancelled_ReportsAConflict()
    {
        await SignUpAsync();
        var reservation = (await Api.Reservations.CreateAsync(Booking((await FindCarAsync("RAV4")).Id, 10, 12))).ShouldBeCreated();
        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeNoContent();

        (await Api.Reservations.UpdateAsync(reservation, BookingChange(20, 22)))
            .ShouldBeConflict(ReservationErrors.AlreadyCancelled);
    }

    [Fact]
    public async Task UpdateReservation_AfterTheRentalHasStarted_ReportsAConflict()
    {
        await SignUpAsync();
        var reservation = (await Api.Reservations.CreateAsync(Booking((await FindCarAsync("RAV4")).Id, 10, 12))).ShouldBeCreated();

        await BackdateAsync(reservation.Id);

        (await Api.Reservations.UpdateAsync(reservation, BookingChange(20, 22)))
            .ShouldBeConflict(ReservationErrors.AlreadyStarted);
    }

    [Fact]
    public async Task UpdateReservation_WithAnUnknownId_ReportsNotFound()
    {
        await SignUpAsync();

        (await Api.Reservations.UpdateAsync(Guid.NewGuid(), BookingChange(20, 22), ifMatch: "1"))
            .ShouldBeNotFound(ReservationErrors.NotFound);
    }

    [Fact]
    public async Task UpdateReservation_WithAPickupDateInThePast_ReportsAValidationError()
    {
        await SignUpAsync();
        var reservation = (await Api.Reservations.CreateAsync(Booking((await FindCarAsync("RAV4")).Id, 10, 12))).ShouldBeCreated();

        (await Api.Reservations.UpdateAsync(reservation, BookingChange(-2, 5))).ShouldFailValidationOn("startDate");
    }
    
    [Fact]
    public async Task CancelReservation_BeforeItStarts_MarksItCancelledAndStampsTheTime()
    {
        await SignUpAsync();
        var reservation = (await Api.Reservations.CreateAsync(Booking((await FindCarAsync("RAV4")).Id, 10, 12))).ShouldBeCreated();

        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeNoContent();

        var stored = await Factory.WithDbAsync(db => db.Reservations.SingleAsync(r => r.Id == reservation.Id));

        stored.Status.ShouldBe(ReservationStatus.Cancelled);
        stored.CancelledAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task CancelReservation_WhenAlreadyCancelled_ReportsAConflict()
    {
        await SignUpAsync();
        var reservation = (await Api.Reservations.CreateAsync(Booking((await FindCarAsync("RAV4")).Id, 10, 12))).ShouldBeCreated();
        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeNoContent();

        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeConflict(ReservationErrors.AlreadyCancelled);
    }

    [Fact]
    public async Task CancelReservation_AfterTheRentalHasStarted_ReportsAConflict()
    {
        await SignUpAsync();
        var reservation = (await Api.Reservations.CreateAsync(Booking((await FindCarAsync("RAV4")).Id, 10, 12))).ShouldBeCreated();

        await BackdateAsync(reservation.Id);

        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeConflict(ReservationErrors.AlreadyStarted);
    }
    
    private Task<int> BackdateAsync(Guid reservationId) => Factory.WithDbAsync(async db =>
    {
        var stored = await db.Reservations.SingleAsync(r => r.Id == reservationId);
        stored.StartDate = Today.AddDays(-1);
        stored.EndDate = Today.AddDays(2);

        return await db.SaveChangesAsync();
    });
}
