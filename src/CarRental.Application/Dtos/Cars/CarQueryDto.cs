using CarRental.Application.Common;

namespace CarRental.Application.Dtos.Cars;

public sealed record CarQueryDto(
    string? Query = null,
    DateOnly? PickupDate = null,
    DateOnly? ReturnDate = null,
    string? Filters = null,
    string? Sorts = null,
    int Page = Paging.FirstPage,
    int PageSize = Paging.DefaultPageSize);
