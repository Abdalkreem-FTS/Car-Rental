using CarRental.Application.Abstractions;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Shouldly;

namespace CarRental.IntegrationTests.Database;

[Collection(ApiCollection.Name)]
public sealed class StartupMigrationTests(CarRentalApiFactory factory)
{
    [Fact]
    public async Task Startup_WithSeedingTurnedOff_StillCreatesTheSchema()
    {
        var connectionString = await FreshDatabaseAsync("migrations_without_seed");

        await using var host = new HostOn(connectionString, seed: false);
        using var _ = host.CreateClient();

        var probe = new DatabaseProbe(connectionString);

        var cars = await probe.QuerySingleAsync<long>(
            """SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Cars'""");

        cars.ShouldBe(1, "seeding is off, but the schema still has to be there");

        (await probe.QuerySingleAsync<long>("""SELECT count(*) FROM "Cars" """)).ShouldBe(0);
    }

    [Fact]
    public async Task Startup_WithMigrationTurnedOff_LeavesTheDatabaseAlone()
    {
        var connectionString = await FreshDatabaseAsync("no_migrations_at_all");

        await using var host = new HostOn(connectionString, seed: false, migrate: false);
        using var _ = host.CreateClient();

        var tables = await new DatabaseProbe(connectionString).QuerySingleAsync<long>(
            "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'");

        tables.ShouldBe(0, "migrating on startup is its own decision, and it was declined");
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

    private sealed class HostOn(string connectionString, bool seed, bool migrate = true) : WebApplicationFactory<Program>
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
                    ["Database:MigrateOnStartup"] = migrate ? "true" : "false",
                    ["Seed:Enabled"] = seed ? "true" : "false",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(new CapturingEmailSender());
            });
        }
    }
}
