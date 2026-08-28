using System.Net;
using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Auth;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class RateLimitTests(CarRentalApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ForgotPassword_OnceTheWindowIsSpent_IsRefusedWithRetryAfter()
    {
        await using var host = Limited(auth: 2);
        using var api = new CarRentalApi(host.CreateClient());

        (await api.Auth.ForgotPasswordAsync("someone@example.com")).StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await api.Auth.ForgotPasswordAsync("someone@example.com")).StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var refused = await api.Auth.ForgotPasswordAsync("someone@example.com");

        refused.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        refused.Problem!.ErrorCode.ShouldBe("rate_limit_exceeded");
        refused.Problem.Errors.ShouldBeNull("a document must not carry both conventions at once");
    }

    [Fact]
    public async Task Register_OnceTheWindowIsSpent_IsRefused()
    {
        await using var host = Limited(accounts: 1);
        using var api = new CarRentalApi(host.CreateClient());

        (await api.Auth.RegisterAsync(TestData.Registration())).StatusCode.ShouldBe(HttpStatusCode.Created);

        var refused = await api.Auth.RegisterAsync(TestData.Registration());

        refused.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Search_OnceTheBucketIsEmpty_IsRefused()
    {
        await using var host = Limited(searchBurst: 3);
        using var api = new CarRentalApi(host.CreateClient());

        api.Authenticate(await TokenAsync(host));

        for (var i = 0; i < 3; i++)
        {
            (await api.Cars.SearchAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        (await api.Cars.SearchAsync()).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Search_WhenAnotherAccountHasSpentItsAllowance_IsUnaffected()
    {
        await using var host = Limited(searchBurst: 2);

        using var heavy = new CarRentalApi(host.CreateClient());
        heavy.Authenticate(await TokenAsync(host));

        using var quiet = new CarRentalApi(host.CreateClient());
        quiet.Authenticate(await TokenAsync(host));

        (await heavy.Cars.SearchAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await heavy.Cars.SearchAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await heavy.Cars.SearchAsync()).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        (await quiet.Cars.SearchAsync()).StatusCode.ShouldBe(
            HttpStatusCode.OK,
            "allowances are per account, so one caller cannot spend another's");
    }

    private static async Task<string> TokenAsync(WebApplicationFactory<Program> host)
    {
        using var api = new CarRentalApi(host.CreateClient());

        var auth = (await api.Auth.RegisterAsync(TestData.Registration())).ShouldBeCreated();

        return auth.AccessToken;
    }

    private LimitedHost Limited(int auth = 1000, int accounts = 1000, int searchBurst = 1000) =>
        new(factory.ConnectionString, auth, accounts, searchBurst);

    private sealed class LimitedHost(string connectionString, int auth, int accounts, int searchBurst)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = connectionString,
                    ["Jwt:Key"] = "integration-tests-signing-key-at-least-32-characters-long",
                    ["Jwt:Issuer"] = "CarRental.Api",
                    ["Jwt:Audience"] = "CarRental.Client",
                    ["ClientApp:BaseUrl"] = "https://rentals.example.test",
                    ["Seed:Enabled"] = "false",
                    ["Database:MigrateOnStartup"] = "false",
                    ["RateLimiting:Enabled"] = "true",
                    ["RateLimiting:Auth:Permit"] = auth.ToString(),
                    ["RateLimiting:Auth:WindowMinutes"] = "15",
                    ["RateLimiting:Accounts:Permit"] = accounts.ToString(),
                    ["RateLimiting:Accounts:WindowMinutes"] = "5",
                    ["RateLimiting:Search:Burst"] = searchBurst.ToString(),
                    ["RateLimiting:Search:PerMinute"] = "1",
                    ["RateLimiting:Global:Permit"] = "10000",
                    ["RateLimiting:Global:WindowMinutes"] = "1",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(new CapturingEmailSender());
            });
        }
    }
}
