namespace CarRental.Application.Contracts.Cars;

/// <param name="Query">Free text matched against make, model and location.</param>
/// <param name="PickupDate">Together with <paramref name="ReturnDate"/>, keeps only cars free for the whole span.</param>
/// <param name="ReturnDate">Together with <paramref name="PickupDate"/>, keeps only cars free for the whole span.</param>
/// <param name="Filters">A Sieve filter expression, for example <c>category==SUV,dailyRate&lt;=60</c>.</param>
/// <param name="Sorts">A Sieve sort expression, for example <c>-dailyRate,year</c>.</param>
/// <param name="Page">One-based page number.</param>
/// <param name="PageSize">How many cars to return per page.</param>
public sealed record CarQueryRequest(
    string? Query = null,
    DateOnly? PickupDate = null,
    DateOnly? ReturnDate = null,
    string? Filters = null,
    string? Sorts = null,
    int Page = 1,
    int PageSize = 12);
