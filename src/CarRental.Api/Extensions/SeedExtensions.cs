using CarRental.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace CarRental.Api.Extensions;

public static class SeedExtensions
{
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<SeedOptions>>().Value;

        if (!options.Enabled)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();

        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        await seeder.SeedAsync();
    }
}
