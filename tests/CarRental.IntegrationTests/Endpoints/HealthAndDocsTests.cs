using System.Net;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class HealthAndDocsTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Health_WithoutAToken_ReportsHealthy()
    {
        (await Api.HealthAsync()).ShouldBeOk().Status.ShouldBe("healthy");
    }

    [Fact]
    public async Task Startup_WhenTheHostBoots_AppliesMigrationsAndSeedsTheFleet()
    {
        var cars = await Factory.WithDbAsync(db => Task.FromResult(db.Cars.Count()));

        cars.ShouldBe(TestData.FleetSize);
    }

    [Fact]
    public async Task Request_ForAnUnknownRoute_ReturnsAProblemWithTheCustomisedDetail()
    {
        var response = await Api.SendAsync(HttpMethod.Get, "/api/does-not-exist");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Problem!.Detail.ShouldBe("No endpoint matches this URL.");
    }

    [Fact]
    public async Task Request_WithAnUnsupportedMethod_ReturnsAProblemWithTheCustomisedDetail()
    {
        var response = await Api.SendAsync(HttpMethod.Delete, Routes.Health);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        response.Problem!.Detail.ShouldBe("This endpoint does not accept that HTTP method.");
    }

    [Fact]
    public async Task Request_WithMalformedJson_ReturnsAProblemFromTheGlobalHandler()
    {
        var response = await Api.PostRawAsync(Routes.Auth.Login, "{\"email\":");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Problem!.Detail.ShouldNotBeNullOrWhiteSpace();
    }
}
