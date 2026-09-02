using CarRental.Api.Contracts.Cars;
using CarRental.Domain.Enums;
using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Support.Api;
using CarRental.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class CarEndpointTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Browse_WithoutAToken_ReportsUnauthorized()
    {
        (await Api.Cars.SearchAsync()).ShouldRequireAuthentication();
        (await Api.Cars.LocationsAsync()).ShouldRequireAuthentication();
    }

    [Fact]
    public async Task Browse_WithAnUnreadableToken_ReportsUnauthorized()
    {
        Api.Authenticate("not.a.jwt");

        (await Api.Cars.SearchAsync()).ShouldRequireAuthentication();
    }

    [Fact]
    public async Task Search_WithNoFilters_ReturnsTheSeededFleetPaged()
    {
        await SignUpAsync();

        var page = (await Api.Cars.SearchAsync()).ShouldBeOk();

        page.TotalCount.ShouldBe(TestData.FleetSize);
        page.Page.ShouldBe(1);
        page.PageSize.ShouldBe(12);
        page.Items.Count.ShouldBe(12);
        page.TotalPages.ShouldBe(2);
        page.HasNext.ShouldBeTrue();
        page.HasPrevious.ShouldBeFalse();
    }

    [Fact]
    public async Task Search_WhenPagingThroughTheFleet_ReturnsEveryCarExactlyOnce()
    {
        await SignUpAsync();

        var first = (await Api.Cars.SearchAsync(new CarQuery { Page = 1, PageSize = 10 })).ShouldBeOk();
        var second = (await Api.Cars.SearchAsync(new CarQuery { Page = 2, PageSize = 10 })).ShouldBeOk();

        first.Items.Count.ShouldBe(10);
        second.Items.Count.ShouldBe(TestData.FleetSize - 10);
        second.HasNext.ShouldBeFalse();
        second.HasPrevious.ShouldBeTrue();

        first.Items.Concat(second.Items).Select(car => car.Id).Distinct().Count().ShouldBe(TestData.FleetSize);
    }

    [Theory]
    [InlineData(CarCategory.SUV)]
    [InlineData(CarCategory.Van)]
    [InlineData(CarCategory.Economy)]
    public async Task Search_WithACategoryFilter_ReturnsOnlyThatCategory(CarCategory category)
    {
        await SignUpAsync();

        var page = (await Api.Cars.SearchAsync(CarQuery.All with { Category = category })).ShouldBeOk();

        page.Items.ShouldNotBeEmpty();
        page.Items.ShouldAllBe(car => car.Category == category);
    }

    [Fact]
    public async Task Search_WithTransmissionAndFuelFilters_AppliesBoth()
    {
        await SignUpAsync();

        var page = (await Api.Cars.SearchAsync(CarQuery.All with
        {
            Transmission = TransmissionType.Manual,
            Fuel = FuelType.Diesel,
        })).ShouldBeOk();

        page.Items.ShouldNotBeEmpty();
        page.Items.ShouldAllBe(car => car.Transmission == TransmissionType.Manual && car.Fuel == FuelType.Diesel);
    }

    [Fact]
    public async Task Search_WithSeatAndPriceFilters_TreatsThemAsInclusiveBounds()
    {
        await SignUpAsync();

        var page = (await Api.Cars.SearchAsync(CarQuery.All with { MinSeats = 7, MaxDailyRate = 80m })).ShouldBeOk();

        page.Items.ShouldNotBeEmpty();
        page.Items.ShouldAllBe(car => car.Seats >= 7 && car.DailyRate <= 80m);
    }

    [Fact]
    public async Task Search_WithFreeText_MatchesMakeModelOrLocationCaseInsensitively()
    {
        await SignUpAsync();

        var byModel = (await Api.Cars.SearchAsync(CarQuery.Matching("rav4"))).ShouldBeOk();
        var byMake = (await Api.Cars.SearchAsync(CarQuery.Matching("TOYOTA"))).ShouldBeOk();
        var byLocation = (await Api.Cars.SearchAsync(CarQuery.Matching("aqaba"))).ShouldBeOk();

        byModel.Items.ShouldHaveSingleItem().Model.ShouldBe("RAV4");
        byMake.Items.ShouldAllBe(car => car.Make == "Toyota");
        byLocation.Items.ShouldAllBe(car => car.Location == "Aqaba");
    }

    [Fact]
    public async Task Search_WithALocationFilter_MatchesExactlyRatherThanBySubstring()
    {
        await SignUpAsync();

        var page = (await Api.Cars.SearchAsync(CarQuery.All with { Location = "Amman" })).ShouldBeOk();

        page.Items.ShouldNotBeEmpty();
        page.Items.ShouldAllBe(car => car.Location == "Amman");
    }

    [Fact]
    public async Task Search_WithASortKey_OrdersByThatKey()
    {
        await SignUpAsync();

        var ascending = (await Api.Cars.SearchAsync(CarQuery.All with { SortBy = "price_asc" })).ShouldBeOk();
        var descending = (await Api.Cars.SearchAsync(CarQuery.All with { SortBy = "price_desc" })).ShouldBeOk();

        ascending.Items.Select(car => car.DailyRate).ShouldBe(ascending.Items.Select(car => car.DailyRate).Order());
        descending.Items.Select(car => car.DailyRate).ShouldBe(descending.Items.Select(car => car.DailyRate).OrderDescending());
        descending.Items[0].DailyRate.ShouldBeGreaterThan(ascending.Items[0].DailyRate);
    }

    [Fact]
    public async Task Search_WithFiltersThatMatchNothing_ReturnsAnEmptyPage()
    {
        await SignUpAsync();

        var page = (await Api.Cars.SearchAsync(new CarQuery { MaxDailyRate = 1m })).ShouldBeOk();

        page.TotalCount.ShouldBe(0);
        page.Items.ShouldBeEmpty();
        page.TotalPages.ShouldBe(0);
    }

    [Theory]
    [InlineData("pageSize=999", "pageSize")]
    [InlineData("page=0", "page")]
    [InlineData("minSeats=99", "minSeats")]
    [InlineData("minDailyRate=100&maxDailyRate=10", "maxDailyRate")]
    public async Task Search_WithAnOutOfRangeQueryString_ReportsAValidationError(string queryString, string expectedField)
    {
        await SignUpAsync();

        var response = await Api.Cars.SearchRawAsync(queryString);

        response.ShouldFailValidationOn(expectedField);
    }

    [Fact]
    public async Task Search_WithAReturnDateBeforeThePickupDate_ReportsAValidationError()
    {
        await SignUpAsync();

        var response = await Api.Cars.SearchAsync(new CarQuery
        {
            PickupDate = TestData.In(10),
            ReturnDate = TestData.In(3),
        });

        response.ShouldFailValidationOn("returnDate");
    }

    [Theory]
    [InlineData("Sharm, South")]
    [InlineData("Wadi|Rum")]
    public async Task Search_ByALocationCarryingASieveOperator_TreatsItAsPlainText(string location)
    {
        await SignInAsAdminAsync();
        (await Api.Cars.CreateAsync(TestData.NewCar() with { Location = location })).ShouldBeCreated();

        var page = (await Api.Cars.SearchAsync(CarQuery.All with { Location = location })).ShouldBeOk();

        page.Items.ShouldHaveSingleItem().Location.ShouldBe(location);
    }

    [Fact]
    public async Task GetLocations_WhenCalled_ListsEachServedCityOnceInOrder()
    {
        await SignUpAsync();

        var locations = (await Api.Cars.LocationsAsync()).ShouldBeOk();

        locations.ShouldBe(["Amman", "Aqaba", "Dead Sea", "Irbid", "Zarqa"]);
    }

    [Fact]
    public async Task GetLocations_AfterEveryCarInACityIsRetired_OmitsThatCity()
    {
        await SignInAsAdminAsync();

        var zarqaCars = await Factory.WithDbAsync(db => db.Cars.Where(car => car.Location == "Zarqa").ToListAsync());

        foreach (var car in zarqaCars)
        {
            (await Api.Cars.RetireAsync(car.Id)).ShouldBeOk();
        }

        (await Api.Cars.LocationsAsync()).ShouldBeOk().ShouldNotContain("Zarqa");
    }

    [Fact]
    public async Task GetCar_WithAKnownId_ReturnsTheCar()
    {
        await SignUpAsync();
        var expected = await FindCarAsync("Wrangler");

        var car = (await Api.Cars.GetAsync(expected.Id)).ShouldBeOk();

        car.Id.ShouldBe(expected.Id);
        car.Make.ShouldBe("Jeep");
        car.Category.ShouldBe(CarCategory.SUV);
    }

    [Fact]
    public async Task GetCar_WithAnUnknownId_ReportsNotFound()
    {
        await SignUpAsync();

        (await Api.Cars.GetAsync(Guid.NewGuid())).ShouldBeNotFound(CarErrors.NotFound);
    }

    [Fact]
    public async Task ManageFleet_AsACustomer_ReportsForbidden()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Picanto");

        (await Api.Cars.CreateAsync(TestData.NewCar())).ShouldBeForbidden();
        (await Api.Cars.UpdateAsync(car.Id, TestData.CarUpdate(TestData.NewCar()))).ShouldBeForbidden();
        (await Api.Cars.RetireAsync(car.Id)).ShouldBeForbidden();
    }

    [Fact]
    public async Task CreateCar_AsAnAdmin_AddsItToTheFleetAndMakesItSearchable()
    {
        await SignInAsAdminAsync();

        var response = await Api.Cars.CreateAsync(TestData.NewCar() with { PlateNumber = "amm-7777" });

        var created = response.ShouldBeCreated();
        created.PlateNumber.ShouldBe("AMM-7777", "plates are normalised to upper case");
        response.Location!.ToString().ShouldBe($"/api/cars/{created.Id}");

        var found = (await Api.Cars.SearchAsync(CarQuery.Matching("CX-5"))).ShouldBeOk();
        found.Items.ShouldHaveSingleItem().Id.ShouldBe(created.Id);
    }

    [Fact]
    public async Task CreateCar_WithAPlateAlreadyInUse_ReportsAConflict()
    {
        await SignInAsAdminAsync();
        (await Api.Cars.CreateAsync(TestData.NewCar() with { PlateNumber = "DUP-001" })).ShouldBeCreated();

        var response = await Api.Cars.CreateAsync(TestData.NewCar() with { PlateNumber = "dup-001" });

        response.ShouldBeConflict(CarErrors.PlateAlreadyInUse("DUP-001"));
    }

    [Fact]
    public async Task UpdateCar_AsAnAdmin_SavesTheChanges()
    {
        await SignInAsAdminAsync();
        var car = await FindCarAsync("Picanto");

        var request = TestData.CarUpdate(TestData.NewCar()) with
        {
            Make = car.Make,
            Model = car.Model,
            PlateNumber = car.PlateNumber,
            DailyRate = 31m,
        };

        (await Api.Cars.UpdateAsync(car.Id, request)).ShouldBeOk().DailyRate.ShouldBe(31m);
    }

    [Fact]
    public async Task UpdateCar_KeepingItsOwnPlate_Succeeds()
    {
        await SignInAsAdminAsync();
        var car = await FindCarAsync("Corolla");

        var request = TestData.CarUpdate(TestData.NewCar()) with
        {
            Make = car.Make,
            Model = car.Model,
            PlateNumber = car.PlateNumber,
        };

        (await Api.Cars.UpdateAsync(car.Id, request)).ShouldBeOk();
    }

    [Fact]
    public async Task GetCar_ForARetiredCar_ReportsNotFoundRatherThanRenderingIt()
    {
        await SignInAsAdminAsync();
        var car = await FindCarAsync("Picanto");
        (await Api.Cars.RetireAsync(car.Id)).ShouldBeOk();

        await SignUpAsync();

        (await Api.Cars.GetAsync(car.Id)).ShouldBeNotFound(CarErrors.NotFound);
    }

    [Fact]
    public async Task UpdateCar_WhenTheBodyOmitsAvailability_LeavesTheCarInTheFleet()
    {
        await SignInAsAdminAsync();
        var car = await FindCarAsync("Elantra");

        // A form that edits the rate and sends nothing else. isActive used to default to false
        // here and quietly retire the car.
        var response = await Api.PutOffContractAsync<CarResponse>(
            Routes.Cars.AdminById(car.Id),
            new
            {
                car.Make,
                car.Model,
                car.Year,
                car.PlateNumber,
                car.Location,
                DailyRate = 41m,
                car.Seats,
                Category = car.Category.ToString(),
                Transmission = car.Transmission.ToString(),
                Fuel = car.Fuel.ToString(),
                car.ImageUrl,
                car.Description,
            });

        var updated = response.ShouldBeOk();

        updated.DailyRate.ShouldBe(41m);
        updated.IsActive.ShouldBeTrue("editing a rate must not retire the car");

        (await Api.Cars.GetAsync(car.Id)).ShouldBeOk();
    }

    [Fact]
    public async Task Reinstate_ForARetiredCar_BringsItBackIntoTheFleet()
    {
        await SignInAsAdminAsync();
        var car = await FindCarAsync("Kicks");
        (await Api.Cars.RetireAsync(car.Id)).ShouldBeOk();

        (await Api.Cars.ReinstateAsync(car.Id)).ShouldBeOk().IsActive.ShouldBeTrue();

        (await Api.Cars.GetAsync(car.Id)).ShouldBeOk();
    }

    [Fact]
    public async Task CreateCar_WithARetiredCarsPlate_StillReportsTheConflict()
    {
        await SignInAsAdminAsync();
        var car = await FindCarAsync("Accent");
        (await Api.Cars.RetireAsync(car.Id)).ShouldBeOk();

        var response = await Api.Cars.CreateAsync(TestData.NewCar() with { PlateNumber = car.PlateNumber });

        response.ShouldBeConflict(CarErrors.PlateAlreadyInUse(car.PlateNumber));
    }

    [Fact]
    public async Task DeleteCar_AsAnAdmin_RetiresItInsteadOfRemovingTheRow()
    {
        await SignInAsAdminAsync();
        var car = await FindCarAsync("Corolla");

        (await Api.Cars.RetireAsync(car.Id)).ShouldBeOk();

        var stored = await Factory.WithDbAsync(db => db.Cars.IgnoreQueryFilters().SingleAsync(c => c.Id == car.Id));
        stored.IsActive.ShouldBeFalse();

        var page = (await Api.Cars.SearchAsync(CarQuery.All)).ShouldBeOk();
        page.Items.ShouldNotContain(c => c.Id == car.Id);
        page.TotalCount.ShouldBe(TestData.FleetSize - 1);
    }

    [Fact]
    public async Task CreateCar_WithInvalidFields_ReportsThemAll()
    {
        await SignInAsAdminAsync();

        var response = await Api.Cars.CreateAsync(TestData.NewCar() with
        {
            Make = "",
            Model = "",
            Year = 1800,
            PlateNumber = "!",
            Location = "",
            DailyRate = 0m,
            Seats = 0,
        });

        response.ShouldFailValidation("make", "model", "year", "plateNumber", "location", "dailyRate", "seats");
    }
}
