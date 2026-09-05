using CarRental.Api.Contracts.Cars;
using CarRental.Domain.Enums;
using CarRental.IntegrationTests.Support;
using CarRental.IntegrationTests.Support.Api;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

/// <summary>
/// The QUERY endpoint: the same fleet as <c>GET /api/cars</c>, asked for with a request body and
/// Sieve expressions instead of a query string.
/// </summary>
public sealed class CarQueryEndpointTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    private static CarQueryRequest EveryCar => new(PageSize: 50);

    [Fact]
    public async Task Query_WithoutAToken_ReportsUnauthorized()
    {
        (await Api.Cars.QueryAsync()).ShouldRequireAuthentication();
    }

    [Fact]
    public async Task Query_WithAnEmptyBody_ReturnsTheSeededFleetPaged()
    {
        await SignUpAsync();

        var page = (await Api.Cars.QueryAsync()).ShouldBeOk();

        page.TotalCount.ShouldBe(TestData.FleetSize);
        page.Page.ShouldBe(1);
        page.PageSize.ShouldBe(12);
        page.Items.Count.ShouldBe(12);
        page.HasNext.ShouldBeTrue();
    }

    [Fact]
    public async Task Query_WithAScalarFilter_NarrowsTheFleet()
    {
        await SignUpAsync();

        var page = (await Api.Cars.QueryAsync(EveryCar with { Filters = "dailyRate<=40" })).ShouldBeOk();

        page.Items.ShouldNotBeEmpty();
        page.Items.ShouldAllBe(car => car.DailyRate <= 40m);
        page.TotalCount.ShouldBeLessThan(TestData.FleetSize);
    }

    [Fact]
    public async Task Query_WithSeveralFilters_AppliesAllOfThem()
    {
        await SignUpAsync();

        var page = (await Api.Cars.QueryAsync(EveryCar with { Filters = "seats>=7,fuel==Diesel" })).ShouldBeOk();

        page.Items.ShouldNotBeEmpty();
        page.Items.ShouldAllBe(car => car.Seats >= 7 && car.Fuel == FuelType.Diesel);
    }

    [Fact]
    public async Task Query_WithAnEnumFilter_MatchesByName()
    {
        await SignUpAsync();

        var page = (await Api.Cars.QueryAsync(EveryCar with { Filters = "category==SUV" })).ShouldBeOk();

        page.Items.ShouldNotBeEmpty();
        page.Items.ShouldAllBe(car => car.Category == CarCategory.SUV);
    }

    [Fact]
    public async Task Query_WithASortExpression_OrdersByIt()
    {
        await SignUpAsync();

        var ascending = (await Api.Cars.QueryAsync(EveryCar with { Sorts = "dailyRate" })).ShouldBeOk();
        var descending = (await Api.Cars.QueryAsync(EveryCar with { Sorts = "-dailyRate" })).ShouldBeOk();

        ascending.Items.Select(car => car.DailyRate).ShouldBe(ascending.Items.Select(car => car.DailyRate).Order());
        descending.Items.Select(car => car.DailyRate).ShouldBe(descending.Items.Select(car => car.DailyRate).OrderDescending());
    }

    [Fact]
    public async Task Query_WithNoSortExpression_FallsBackToAStableDefaultOrder()
    {
        await SignUpAsync();

        var page = (await Api.Cars.QueryAsync(EveryCar)).ShouldBeOk();

        page.Items
            .Select(car => (car.Make, car.Model))
            .ShouldBe(page.Items.Select(car => (car.Make, car.Model)).Order());
    }

    [Fact]
    public async Task Query_WhenPagingWithASortThatTies_ReturnsEveryCarExactlyOnce()
    {
        await SignUpAsync();

        // Every seeded car is active, so this sort key ties across the whole fleet. Paging through
        // it only holds together because of the identifier tiebreak.
        var first = (await Api.Cars.QueryAsync(new CarQueryRequest(Sorts: "seats", Page: 1, PageSize: 8))).ShouldBeOk();
        var second = (await Api.Cars.QueryAsync(new CarQueryRequest(Sorts: "seats", Page: 2, PageSize: 8))).ShouldBeOk();

        first.Items.Concat(second.Items).Select(car => car.Id).Distinct().Count().ShouldBe(TestData.FleetSize);
    }

    [Fact]
    public async Task Query_WithFreeText_StillMatchesMakeModelOrLocation()
    {
        await SignUpAsync();

        var page = (await Api.Cars.QueryAsync(EveryCar with { Query = "aqaba" })).ShouldBeOk();

        page.Items.ShouldNotBeEmpty();
        page.Items.ShouldAllBe(car => car.Location == "Aqaba");
    }

    [Fact]
    public async Task Query_WithFreeTextAndAFilter_AppliesBoth()
    {
        await SignUpAsync();

        var page = (await Api.Cars.QueryAsync(EveryCar with { Query = "amman", Filters = "category==Luxury" })).ShouldBeOk();

        page.Items.ShouldNotBeEmpty();
        page.Items.ShouldAllBe(car => car.Location == "Amman" && car.Category == CarCategory.Luxury);
    }

    [Fact]
    public async Task Query_WithADateSpan_LeavesOutCarsAlreadyBooked()
    {
        await SignUpAsync();

        var car = await FindCarAsync("RAV4");
        (await Api.Reservations.CreateAsync(TestData.Booking(car.Id, 5, 9))).ShouldBeCreated();

        var overlapping = (await Api.Cars.QueryAsync(EveryCar with
        {
            PickupDate = TestData.In(6),
            ReturnDate = TestData.In(7),
        })).ShouldBeOk();

        var clear = (await Api.Cars.QueryAsync(EveryCar with
        {
            PickupDate = TestData.In(20),
            ReturnDate = TestData.In(22),
        })).ShouldBeOk();

        overlapping.Items.ShouldNotContain(candidate => candidate.Id == car.Id);
        clear.Items.ShouldContain(candidate => candidate.Id == car.Id);
    }

    [Fact]
    public async Task Query_WithADateSpan_StillOffersCarsWhoseBookingWasCancelled()
    {
        await SignUpAsync();

        var car = await FindCarAsync("RAV4");
        var reservation = (await Api.Reservations.CreateAsync(TestData.Booking(car.Id, 5, 9))).ShouldBeCreated();

        var whileBooked = (await Api.Cars.QueryAsync(EveryCar with
        {
            PickupDate = TestData.In(6),
            ReturnDate = TestData.In(7),
        })).ShouldBeOk();

        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeNoContent();

        var afterCancelling = (await Api.Cars.QueryAsync(EveryCar with
        {
            PickupDate = TestData.In(6),
            ReturnDate = TestData.In(7),
        })).ShouldBeOk();

        whileBooked.Items.ShouldNotContain(candidate => candidate.Id == car.Id);
        afterCancelling.Items.ShouldContain(candidate => candidate.Id == car.Id);
    }

    [Fact]
    public async Task Query_WithFiltersThatMatchNothing_ReturnsAnEmptyPage()
    {
        await SignUpAsync();

        var page = (await Api.Cars.QueryAsync(EveryCar with { Filters = "dailyRate<1" })).ShouldBeOk();

        page.TotalCount.ShouldBe(0);
        page.Items.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("nonsense==1")]
    [InlineData("plateNumber@=AMM")]
    [InlineData("isActive==false")]
    public async Task Query_FilteringOnAPropertyItMayNotReach_IsRefused(string filters)
    {
        await SignUpAsync();

        var response = await Api.Cars.QueryAsync(EveryCar with { Filters = filters });

        response.ShouldFailValidationOn("filters");
    }

    [Theory]
    [InlineData("nonsense")]
    [InlineData("-description")]
    [InlineData("isActive")]
    public async Task Query_SortingByAPropertyItMayNotReach_IsRefused(string sorts)
    {
        await SignUpAsync();

        var response = await Api.Cars.QueryAsync(EveryCar with { Sorts = sorts });

        response.ShouldFailValidationOn("sorts");
    }

    [Fact]
    public async Task Query_WithAnOutOfRangePageSize_ReportsAValidationError()
    {
        await SignUpAsync();

        (await Api.Cars.QueryAsync(new CarQueryRequest(PageSize: 999))).ShouldFailValidationOn("pageSize");
        (await Api.Cars.QueryAsync(new CarQueryRequest(Page: 0))).ShouldFailValidationOn("page");
    }

    [Fact]
    public async Task Query_WithAReturnDateBeforeThePickupDate_ReportsAValidationError()
    {
        await SignUpAsync();

        var response = await Api.Cars.QueryAsync(EveryCar with
        {
            PickupDate = TestData.In(10),
            ReturnDate = TestData.In(3),
        });

        response.ShouldFailValidationOn("returnDate");
    }

    [Fact]
    public async Task Query_AndGet_AgreeOnTheSameFleet()
    {
        await SignUpAsync();

        var viaGet = (await Api.Cars.SearchAsync(CarQuery.All)).ShouldBeOk();
        var viaQuery = (await Api.Cars.QueryAsync(EveryCar)).ShouldBeOk();

        viaQuery.TotalCount.ShouldBe(viaGet.TotalCount);
        viaQuery.Items.Select(car => car.Id).ShouldBe(viaGet.Items.Select(car => car.Id));
    }
}
