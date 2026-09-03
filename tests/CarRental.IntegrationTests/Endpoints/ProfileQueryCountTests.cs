using CarRental.IntegrationTests.Support;
using CarRental.IntegrationTests.Support.Api;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class ProfileQueryCountTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetProfile_WhenRendered_TakesOneTripToTheDatabase()
    {
        await SignUpAsync();

        Factory.Commands.Reset();

        (await Api.Profile.GetAsync()).ShouldBeOk();

        Factory.Commands.Count.ShouldBe(
            2,
            "one query for the user and their roles, and one to check the token's security stamp is current");
    }
}
