using CarRental.Application.Common;

namespace CarRental.Application.Contracts.Cars;

public sealed record CarQueryRequest(
    string? Query = null,
    DateOnly? PickupDate = null,
    DateOnly? ReturnDate = null,
    string? Filters = null,
    string? Sorts = null,
    int Page = Paging.FirstPage,
    int PageSize = Paging.DefaultPageSize);
