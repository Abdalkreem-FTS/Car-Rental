namespace CarRental.Api.RateLimiting;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    public WindowLimit Auth { get; set; } = new() { Permit = 3, WindowMinutes = 15 };

    public WindowLimit Accounts { get; set; } = new() { Permit = 10, WindowMinutes = 5 };

    public BucketLimit Search { get; set; } = new() { PerMinute = 30, Burst = 10 };

    public WindowLimit Global { get; set; } = new() { Permit = 300, WindowMinutes = 1 };
}

public sealed class WindowLimit
{
    public int Permit { get; set; }

    public int WindowMinutes { get; set; }
}

public sealed class BucketLimit
{
    public int PerMinute { get; set; }

    public int Burst { get; set; }
}
