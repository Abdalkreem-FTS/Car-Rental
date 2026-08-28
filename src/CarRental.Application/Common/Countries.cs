using System.Globalization;

namespace CarRental.Application.Common;

/// <summary>
/// Every country the runtime knows about, read from the platform's region data rather than from a
/// list kept by hand. Adding a country is then somebody else's release, not ours.
/// </summary>
public static class Countries
{
    public static IReadOnlyList<string> All { get; } = Build();

    private static string[] Build() =>
        [.. CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(RegionOrNull)
            .OfType<RegionInfo>()
            .DistinctBy(region => region.TwoLetterISORegionName)
            .Select(region => region.EnglishName)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

    /// <summary>
    /// Not every specific culture maps onto a region, and a host running in globalization-invariant
    /// mode has no region data at all. Either way the culture is simply skipped.
    /// </summary>
    private static RegionInfo? RegionOrNull(CultureInfo culture)
    {
        try
        {
            return new RegionInfo(culture.Name);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
