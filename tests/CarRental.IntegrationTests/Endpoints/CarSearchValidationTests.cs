using CarRental.Api.Contracts.Cars;
using CarRental.IntegrationTests.Support.Api;
using CarRental.IntegrationTests.Support;
using Shouldly;
using static CarRental.IntegrationTests.Support.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class CarSearchValidationTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Search_ForDatesInThePast_IsRefusedRatherThanAnsweredAboutFiveYearsAgo()
    {
        await SignUpAsync();

        var response = await Api.Cars.SearchRawAsync("pickupDate=2020-01-01&returnDate=2020-01-05");

        response.ShouldFailValidationOn("pickupDate");
    }

    [Fact]
    public async Task Search_WithOnlyAPickupDate_IsRefusedRatherThanReturningTheWholeFleet()
    {
        await SignUpAsync();

        var response = await Api.Cars.SearchAsync(CarQuery.All with { PickupDate = In(5) });

        response.ShouldFailValidationOn("returnDate");
    }

    [Fact]
    public async Task Search_WithOnlyAReturnDate_IsRefused()
    {
        await SignUpAsync();

        var response = await Api.Cars.SearchAsync(CarQuery.All with { ReturnDate = In(5) });

        response.ShouldFailValidationOn("pickupDate");
    }

    [Fact]
    public async Task Search_WithBothDates_StillWorks()
    {
        await SignUpAsync();

        (await Api.Cars.SearchAsync(CarQuery.All with { PickupDate = In(5), ReturnDate = In(7) })).ShouldBeOk();
    }

    [Theory]
    [InlineData("whatever")]
    [InlineData("price")]
    [InlineData("PRICE_ASCENDING")]
    public async Task Search_WithASortKeyWeDoNotSupport_SaysSoRatherThanQuietlyIgnoringIt(string sortBy)
    {
        await SignUpAsync();

        (await Api.Cars.SearchAsync(CarQuery.All with { SortBy = sortBy })).ShouldFailValidationOn("sortBy");
    }

    [Theory]
    [InlineData("price_asc")]
    [InlineData("PRICE_DESC")]
    [InlineData("year_desc")]
    [InlineData("seats_desc")]
    public async Task Search_WithASortKeyWeSupport_IsAccepted(string sortBy)
    {
        await SignUpAsync();

        (await Api.Cars.SearchAsync(CarQuery.All with { SortBy = sortBy })).ShouldBeOk();
    }

    [Fact]
    public async Task Search_WithTooLargeAPageSize_IsRefusedRatherThanQuietlyTrimmed()
    {
        await SignUpAsync();

        var response = await Api.Cars.SearchRawAsync("pageSize=500");

        response.ShouldFailValidationOn("pageSize");
    }

    [Theory]
    [InlineData("a")]
    [InlineData("bm")]
    [InlineData("  b  ")]
    public async Task Search_ForATermTooShortToFormATrigram_IsRefused(string term)
    {
        await SignUpAsync();

        var response = await Api.Cars.SearchAsync(CarQuery.All with { Query = term });

        response.ShouldFailValidationOn("query");
    }

    [Fact]
    public async Task Search_ForATermLongEnoughToUseTheIndex_IsAnswered()
    {
        await SignUpAsync();

        (await Api.Cars.SearchAsync(CarQuery.All with { Query = "civ" })).ShouldBeOk();
    }

    [Fact]
    public async Task Query_ForATermTooShortToFormATrigram_IsRefused()
    {
        await SignUpAsync();

        var response = await Api.Cars.QueryAsync(new CarQueryRequest(Query: "bm"));

        response.ShouldFailValidationOn("query");
    }
}
