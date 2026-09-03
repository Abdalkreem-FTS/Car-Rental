using CarRental.IntegrationTests.Support;
using CarRental.IntegrationTests.Support.Api;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class CountryEndpointTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Countries_WithoutAToken_IsReadable()
    {
        // The sign-up form needs the list before anyone has an account.
        var countries = (await Api.CountriesAsync()).ShouldBeOk();

        countries.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Countries_WhenCalled_CoversTheWorldRatherThanAHandPickedFew()
    {
        var countries = (await Api.CountriesAsync()).ShouldBeOk();

        // The handwritten list this replaced held sixty.
        countries.Count.ShouldBeGreaterThan(150);
        countries.ShouldContain("Jordan");
        countries.ShouldContain("United States");
    }

    [Fact]
    public async Task Countries_WhenCalled_IsSortedAndFreeOfDuplicates()
    {
        var countries = (await Api.CountriesAsync()).ShouldBeOk();

        countries.ShouldBe(countries.Order(StringComparer.Ordinal));
        countries.Distinct(StringComparer.Ordinal).Count().ShouldBe(countries.Count);
    }

    [Fact]
    public async Task Countries_WhenAskedTwice_AnswersTheSameBothTimes()
    {
        var first = (await Api.CountriesAsync()).ShouldBeOk();
        var second = (await Api.CountriesAsync()).ShouldBeOk();

        second.ShouldBe(first);
    }
}
