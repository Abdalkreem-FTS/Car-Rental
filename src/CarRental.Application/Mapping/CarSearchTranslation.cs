using System.Globalization;
using System.Text;
using CarRental.Application.Dtos.Cars;

namespace CarRental.Application.Mapping;

/// <summary>
/// Turns the typed query string of <c>GET /api/cars</c> into the Sieve expressions that
/// <c>QUERY /api/cars</c> takes, so both endpoints run through one search engine instead of two
/// implementations that can drift apart.
/// </summary>
public static class CarSearchTranslation
{
    public static CarQueryDto ToQuery(this CarSearchDto request) => new(
        Query: request.Query,
        PickupDate: request.PickupDate,
        ReturnDate: request.ReturnDate,
        Filters: BuildFilters(request),
        Sorts: BuildSorts(request.SortBy),
        Page: request.Page,
        PageSize: request.PageSize);

    private static string? BuildFilters(CarSearchDto request)
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            filters.Add($"location=={Escape(request.Location)}");
        }

        if (request.Category is { } category)
        {
            filters.Add($"category=={category}");
        }

        if (request.Transmission is { } transmission)
        {
            filters.Add($"transmission=={transmission}");
        }

        if (request.Fuel is { } fuel)
        {
            filters.Add($"fuel=={fuel}");
        }

        if (request.MinSeats is { } minSeats)
        {
            filters.Add($"seats>={minSeats.ToString(CultureInfo.InvariantCulture)}");
        }

        if (request.MinDailyRate is { } minDailyRate)
        {
            filters.Add($"dailyRate>={minDailyRate.ToString(CultureInfo.InvariantCulture)}");
        }

        if (request.MaxDailyRate is { } maxDailyRate)
        {
            filters.Add($"dailyRate<={maxDailyRate.ToString(CultureInfo.InvariantCulture)}");
        }

        return filters.Count == 0 ? null : string.Join(',', filters);
    }

    /// <summary>
    /// The sort keys this endpoint has always accepted, expressed as Sieve sorts. An unrecognised
    /// key yields no sort at all, which leaves the default order in place — the same thing the
    /// endpoint did before.
    /// </summary>
    private static string? BuildSorts(string? sortBy) => sortBy?.ToLowerInvariant() switch
    {
        "price_asc" => "dailyRate",
        "price_desc" => "-dailyRate",
        "year_desc" => "-year",
        "seats_desc" => "-seats",
        _ => null,
    };

    /// <summary>
    /// Sieve reads a comma as the end of one filter and a pipe as an alternative value, so a
    /// location carrying either has to be escaped or it would silently become a different query.
    /// </summary>
    private static string Escape(string value)
    {
        var escaped = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (character is ',' or '|' or '\\')
            {
                escaped.Append('\\');
            }

            escaped.Append(character);
        }

        return escaped.ToString();
    }
}
