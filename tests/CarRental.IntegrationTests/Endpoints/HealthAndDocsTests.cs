using System.Net;
using CarRental.IntegrationTests.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using CarRental.IntegrationTests.Infrastructure;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class HealthAndDocsTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Health_WithoutAToken_ReportsHealthy()
    {
        var health = (await Api.HealthAsync()).ShouldBeOk();

        health.Status.ShouldBe("healthy");
        health.Checks.ShouldContainKeyAndValue("database", "Healthy");
    }

    [Fact]
    public async Task Health_WhenACheckFails_ReportsUnhealthyAndRefusesTraffic()
    {
        using var factory = Factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services
                .AddHealthChecks()
                .AddCheck("boom", () => HealthCheckResult.Unhealthy("pretend the database went away"))));

        using var api = new CarRentalApi(factory.CreateClient());

        var response = await api.HealthAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.RawBody.ShouldContain("\"status\":\"unhealthy\"");
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
        await SignUpAsync();

        var response = await Api.SendAsync(HttpMethod.Get, "/api/does-not-exist");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Problem!.Detail.ShouldBe("No endpoint matches this URL.");
    }

    [Fact]
    public async Task Request_WithAnUnsupportedMethod_ReturnsAProblemWithTheCustomisedDetail()
    {
        await SignUpAsync();

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

    [Fact]
    public async Task Request_ForAnUnknownRoute_WithoutAToken_RefusesRatherThanConfirmingTheRouteIsMissing()
    {
        var response = await Api.SendAsync(HttpMethod.Get, "/api/does-not-exist");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
