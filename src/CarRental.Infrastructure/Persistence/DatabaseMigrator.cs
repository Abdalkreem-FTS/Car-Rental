using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarRental.Infrastructure.Persistence;

public sealed class DatabaseMigrator(AppDbContext context, ILogger<DatabaseMigrator> logger)
{
    private const long LockKey = 8_263_517_940_112_233;

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var database = context.Database;

        await database.OpenConnectionAsync(cancellationToken);

        try
        {
            await database.ExecuteSqlRawAsync($"SELECT pg_advisory_lock({LockKey})", cancellationToken);

            var pending = (await database.GetPendingMigrationsAsync(cancellationToken)).ToList();

            if (pending.Count == 0)
            {
                logger.LogInformation("Database schema is up to date");

                return;
            }

            logger.LogInformation("Applying {Count} migration(s): {Migrations}", pending.Count, string.Join(", ", pending));

            await database.MigrateAsync(cancellationToken);
        }
        finally
        {
            await database.ExecuteSqlRawAsync($"SELECT pg_advisory_unlock({LockKey})", cancellationToken);
            await database.CloseConnectionAsync();
        }
    }
}
