using CarRental.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace CarRental.Api.Extensions;

public static class SeedExtensions
{
    extension(WebApplication app)
    {
        public async Task MigrateDatabaseAsync()
        {
            if (!app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value.MigrateOnStartup)
            {
                return;
            }

            await using var scope = app.Services.CreateAsyncScope();

            await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().MigrateAsync();
        }

        public async Task SeedDatabaseAsync()
        {
            if (!app.Services.GetRequiredService<IOptions<SeedOptions>>().Value.Enabled)
            {
                return;
            }

            await using var scope = app.Services.CreateAsyncScope();

            await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
        }
    }
}
