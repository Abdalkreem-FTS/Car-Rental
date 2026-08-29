using CarRental.Application.Abstractions;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Shouldly;

namespace CarRental.IntegrationTests.Database;

[Collection(ApiCollection.Name)]
public sealed class RegistrationWithoutRolesTests(CarRentalApiFactory factory)
{
    [Fact]
    public async Task Register_WhenTheCustomerRoleDoesNotExist_FailsWithoutLeavingAnAccountBehind()
    {
        var connectionString = await FreshDatabaseAsync("registration_without_roles");

        await using var host = new HostOn(connectionString);
        using var api = new CarRentalApi(host.CreateClient());

        var registration = TestData.Registration();

        var response = await api.Auth.RegisterAsync(registration);

        response.IsSuccess.ShouldBeFalse(
            $"no role means no access to anything, so this must not report success. Body: {response.RawBody}");

        var accounts = await CountUsersAsync(connectionString, registration.Email);

        accounts.ShouldBe(0, "a failed registration must not strand an account whose email can never be reused");

        // The address has to still be usable once the deployment is fixed.
        var second = await api.Auth.RegisterAsync(registration);

        second.IsSuccess.ShouldBeFalse("roles are still missing, but for the same reason as before");
    }

    private static async Task<int> CountUsersAsync(string connectionString, string email)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand("""SELECT count(*) FROM "AspNetUsers" WHERE "Email" = @email""", connection);
        command.Parameters.AddWithValue("email", email);

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private async Task<string> FreshDatabaseAsync(string name)
    {
        var builder = new NpgsqlConnectionStringBuilder(factory.ConnectionString);
        var administrative = new NpgsqlConnectionStringBuilder(factory.ConnectionString) { Database = "postgres" };

        await using (var connection = new NpgsqlConnection(administrative.ConnectionString))
        {
            await connection.OpenAsync();

            await using var drop = new NpgsqlCommand($"""DROP DATABASE IF EXISTS "{name}" WITH (FORCE)""", connection);
            await drop.ExecuteNonQueryAsync();

            await using var create = new NpgsqlCommand($"""CREATE DATABASE "{name}" """, connection);
            await create.ExecuteNonQueryAsync();
        }

        builder.Database = name;

        return builder.ConnectionString;
    }

    private sealed class HostOn(string connectionString) : WebApplicationFactory<Program>
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
                    ["RateLimiting:Enabled"] = "false",
                    ["Seed:Enabled"] = "false",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(new CapturingEmailSender());
            });
        }
    }
}
