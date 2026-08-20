using CarRental.Domain.Enums;

namespace CarRental.IntegrationTests.Api;

public sealed record CarQuery
{
    public string? Query { get; init; }

    public string? Location { get; init; }

    public DateOnly? PickupDate { get; init; }

    public DateOnly? ReturnDate { get; init; }

    public CarCategory? Category { get; init; }

    public TransmissionType? Transmission { get; init; }

    public FuelType? Fuel { get; init; }

    public int? MinSeats { get; init; }

    public decimal? MinDailyRate { get; init; }

    public decimal? MaxDailyRate { get; init; }

    public string? SortBy { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public static CarQuery All => new() { PageSize = 50 };

    public static CarQuery Matching(string text) => new() { Query = text };

    internal string ToRoute()
    {
        var parts = new List<string>();

        Add("query", Query);
        Add("location", Location);
        Add("pickupDate", PickupDate?.ToString("yyyy-MM-dd"));
        Add("returnDate", ReturnDate?.ToString("yyyy-MM-dd"));
        Add("category", Category?.ToString());
        Add("transmission", Transmission?.ToString());
        Add("fuel", Fuel?.ToString());
        Add("minSeats", MinSeats?.ToString());
        Add("minDailyRate", MinDailyRate?.ToString());
        Add("maxDailyRate", MaxDailyRate?.ToString());
        Add("sortBy", SortBy);
        Add("page", Page?.ToString());
        Add("pageSize", PageSize?.ToString());

        return parts.Count == 0 ? Routes.Cars.Base : $"{Routes.Cars.Base}?{string.Join('&', parts)}";

        void Add(string name, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                parts.Add($"{name}={Uri.EscapeDataString(value)}");
            }
        }
    }
}
