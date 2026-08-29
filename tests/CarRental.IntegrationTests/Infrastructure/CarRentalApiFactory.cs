using CarRental.Application.Abstractions;
using CarRental.Infrastructure.Email;
using CarRental.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace CarRental.IntegrationTests.Infrastructure;

public sealed class CarRentalApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("carrental_test")
        .WithUsername("carrental")
        .WithPassword("carrental")
        .Build();

    private Respawner _respawner = null!;
    private NpgsqlConnection _resetConnection = null!;

    public string ConnectionString => _postgres.GetConnectionString();

    public CapturingEmailSender Emails { get; } = new();

    public TestClock Clock { get; } = new();

    public CommandCounter Commands { get; } = new();

    public LogRecorder Logs { get; } = new();

    public DatabaseProbe Database => field ??= new DatabaseProbe(ConnectionString);

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        
        using var _ = CreateClient();

        _resetConnection = new NpgsqlConnection(ConnectionString);
        await _resetConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_resetConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Respawn.Graph.Table("public", "__EFMigrationsHistory")],
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(Commands);
            logging.AddProvider(Logs);
            logging.AddFilter(DbLoggerCategory.Database.Command.Name, LogLevel.Information);
        });

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ConnectionString,
                ["Jwt:Key"] = "integration-tests-signing-key-at-least-32-characters-long",
                ["Jwt:Issuer"] = "CarRental.Api",
                ["Jwt:Audience"] = "CarRental.Client",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7",
                ["Jwt:RefreshReuseLeewaySeconds"] = "1",
                ["ClientApp:BaseUrl"] = "https://rentals.example.test",
                ["RateLimiting:Enabled"] = "false",
                ["EmailDelivery:BackgroundDelivery"] = "false",
                ["Seed:Enabled"] = "true",
                ["Seed:AdminEmail"] = TestData.AdminEmail,
                ["Seed:AdminPassword"] = TestData.AdminPassword,
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

        });
    }
    
    public async Task ResetAsync()
    {
        Emails.Clear();
        Logs.Reset();
        Clock.ResetToRealTime();

        await _respawner.ResetAsync(_resetConnection);

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    }

    public async Task<CapturingEmailSender> DeliveredEmailsAsync()
    {
        await DeliverQueuedEmailAsync();

        return Emails;
    }

    public async Task<int> DeliverQueuedEmailAsync()
    {
        await using var scope = Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<EmailDispatcher>().DispatchDueAsync();
    }

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> work)
    {
        await using var scope = Services.CreateAsyncScope();

        return await work(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public new async Task DisposeAsync()
    {
        await _resetConnection.DisposeAsync();

        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
