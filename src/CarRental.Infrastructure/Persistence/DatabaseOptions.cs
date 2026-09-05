namespace CarRental.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public const int DefaultCommandTimeoutSeconds = 30;

    public bool MigrateOnStartup { get; set; } = true;
}
