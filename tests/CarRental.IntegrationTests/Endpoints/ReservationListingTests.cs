using CarRental.Domain.Enums;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Shouldly;
using static CarRental.IntegrationTests.Infrastructure.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class ReservationListingTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task List_WithoutAScope_IsPagedRatherThanReturningEverything()
    {
        await BookSeveralAsync(5);

        var page = (await Api.Reservations.ListAsync(pageSize: 2)).ShouldBeOk();

        page.Items.Count.ShouldBe(2);
        page.TotalCount.ShouldBe(5);
        page.HasNext.ShouldBeTrue();
    }

    [Fact]
    public async Task List_WhenPagingThrough_ReturnsEachReservationOnce()
    {
        await BookSeveralAsync(5);

        var first = (await Api.Reservations.ListAsync(page: 1, pageSize: 3)).ShouldBeOk();
        var second = (await Api.Reservations.ListAsync(page: 2, pageSize: 3)).ShouldBeOk();

        first.Items.Concat(second.Items).Select(reservation => reservation.Id).Distinct().Count().ShouldBe(5);
    }

    [Fact]
    public async Task List_ForUpcoming_LeavesOutWhatIsFinishedOrCancelled()
    {
        var booked = await BookSeveralAsync(3);
        (await Api.Reservations.CancelAsync(booked[0].Id)).ShouldBeNoContent();

        var upcoming = (await Api.Reservations.ListAsync(ReservationScope.Upcoming)).ShouldBeOk();

        upcoming.TotalCount.ShouldBe(2);
        upcoming.Items.ShouldAllBe(reservation => reservation.Status == ReservationStatus.Confirmed);
    }

    [Fact]
    public async Task List_ForPast_ShowsWhatUpcomingLeftOut()
    {
        var booked = await BookSeveralAsync(3);
        (await Api.Reservations.CancelAsync(booked[0].Id)).ShouldBeNoContent();

        var past = (await Api.Reservations.ListAsync(ReservationScope.Past)).ShouldBeOk();

        past.Items.ShouldHaveSingleItem().Status.ShouldBe(ReservationStatus.Cancelled);
    }

    [Fact]
    public async Task List_OnceARentalHasEnded_MovesItFromUpcomingToPast()
    {
        var booked = await BookSeveralAsync(1);

        (await Api.Reservations.ListAsync(ReservationScope.Upcoming)).ShouldBeOk().TotalCount.ShouldBe(1);

        Factory.Clock.Advance(TimeSpan.FromDays(60));

        (await Api.Reservations.ListAsync(ReservationScope.Upcoming)).ShouldBeOk().TotalCount.ShouldBe(0);
        (await Api.Reservations.ListAsync(ReservationScope.Past)).ShouldBeOk().TotalCount.ShouldBe(1);

        booked.Count.ShouldBe(1);
    }

    [Fact]
    public async Task List_WithAnOutOfRangePageSize_ReportsAValidationError()
    {
        await SignUpAsync();

        (await Api.Reservations.ListAsync(pageSize: 999)).ShouldFailValidationOn("pageSize");
    }

    private async Task<List<Application.Contracts.Reservations.ReservationResponse>> BookSeveralAsync(int count)
    {
        await SignUpAsync();
        var car = await FindCarAsync("Hiace");

        var booked = new List<Application.Contracts.Reservations.ReservationResponse>();

        for (var index = 0; index < count; index++)
        {
            booked.Add((await Api.Reservations.CreateAsync(Booking(car.Id, 5 + (index * 4), 7 + (index * 4)))).ShouldBeCreated());
        }

        return booked;
    }
}
