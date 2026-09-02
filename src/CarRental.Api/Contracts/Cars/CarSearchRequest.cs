using CarRental.Api.Mapping;
using CarRental.Application.Common;
using CarRental.Application.Dtos.Cars;
using CarRental.Domain.Enums;

namespace CarRental.Api.Contracts.Cars;

public sealed record CarSearchRequest(
    string? Query = null,
    string? Location = null,
    DateOnly? PickupDate = null,
    DateOnly? ReturnDate = null,
    CarCategory? Category = null,
    TransmissionType? Transmission = null,
    FuelType? Fuel = null,
    int? MinSeats = null,
    decimal? MinDailyRate = null,
    decimal? MaxDailyRate = null,
    string? SortBy = null,
    int Page = Paging.FirstPage,
    int PageSize = Paging.DefaultPageSize) : IRequestContract<CarSearchDto>
{
    public CarSearchDto ToDto() => ContractMappings.Map(this);
}
