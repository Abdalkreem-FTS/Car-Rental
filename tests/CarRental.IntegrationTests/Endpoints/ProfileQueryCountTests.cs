using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
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

        Factory.Commands.Count.ShouldBe(1, "the user and their roles come back in one query");
    }
}
