using System.Globalization;
using CarRental.Application.Contracts.Cars;
using CarRental.Application.Mapping;
using CarRental.Domain.Enums;
using Shouldly;

namespace CarRental.UnitTests;

public sealed class CarSearchTranslationTests
{
    [Fact]
    public void ToQuery_WithNothingFilled_AsksForNoFiltersOrSorts()
    {
        var query = new CarSearchRequest().ToQuery();

        query.Filters.ShouldBeNull();
        query.Sorts.ShouldBeNull();
    }

    [Fact]
    public void ToQuery_WithEveryFilter_WritesThemAllAsOneExpression()
    {
        var query = new CarSearchRequest(
            Location: "Amman",
            Category: CarCategory.SUV,
            Transmission: TransmissionType.Automatic,
            Fuel: FuelType.Hybrid,
            MinSeats: 5,
            MinDailyRate: 20m,
            MaxDailyRate: 80m).ToQuery();

        query.Filters.ShouldBe(
            "location==Amman,category==SUV,transmission==Automatic,fuel==Hybrid,seats>=5,dailyRate>=20,dailyRate<=80");
    }

    [Theory]
    [InlineData("Sharm, South", @"location==Sharm\, South")]
    [InlineData("Wadi|Rum", @"location==Wadi\|Rum")]
    [InlineData(@"Back\Slash", @"location==Back\\Slash")]
    public void ToQuery_WithALocationCarryingASieveOperator_EscapesIt(string location, string expected)
    {
        new CarSearchRequest(Location: location).ToQuery().Filters.ShouldBe(expected);
    }

    [Fact]
    public void ToQuery_WithAFractionalRate_WritesItInvariantly()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

        try
        {
            // A culture that writes a decimal comma would otherwise produce "dailyRate<=12,5",
            // which Sieve reads as two filters.
            new CarSearchRequest(MaxDailyRate: 12.5m).ToQuery().Filters.ShouldBe("dailyRate<=12.5");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData("price_asc", "dailyRate")]
    [InlineData("price_desc", "-dailyRate")]
    [InlineData("year_desc", "-year")]
    [InlineData("seats_desc", "-seats")]
    [InlineData("PRICE_DESC", "-dailyRate")]
    public void ToQuery_WithAKnownSortKey_TranslatesIt(string sortBy, string expected)
    {
        new CarSearchRequest(SortBy: sortBy).ToQuery().Sorts.ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nonsense")]
    [InlineData("plateNumber")]
    public void ToQuery_WithAnUnknownSortKey_AsksForNoSort(string sortBy)
    {
        new CarSearchRequest(SortBy: sortBy).ToQuery().Sorts.ShouldBeNull();
    }

    [Fact]
    public void ToQuery_WhenCalled_CarriesFreeTextDatesAndPagingThroughUntouched()
    {
        var request = new CarSearchRequest(
            Query: "rav4",
            PickupDate: new DateOnly(2026, 5, 1),
            ReturnDate: new DateOnly(2026, 5, 4),
            Page: 3,
            PageSize: 25);

        var query = request.ToQuery();

        query.Query.ShouldBe("rav4");
        query.PickupDate.ShouldBe(new DateOnly(2026, 5, 1));
        query.ReturnDate.ShouldBe(new DateOnly(2026, 5, 4));
        query.Page.ShouldBe(3);
        query.PageSize.ShouldBe(25);
    }
}
