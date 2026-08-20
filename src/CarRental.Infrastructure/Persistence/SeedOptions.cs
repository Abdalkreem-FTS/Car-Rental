namespace CarRental.Infrastructure.Persistence;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; } = true;

    public string AdminEmail { get; set; } = "admin@carrental.local";

    public string AdminPassword { get; set; } = "Admin#12345";
}
