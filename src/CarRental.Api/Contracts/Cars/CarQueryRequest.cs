using CarRental.Api.Mapping;
using CarRental.Application.Common;
using CarRental.Application.Dtos.Cars;

namespace CarRental.Api.Contracts.Cars;

public sealed record CarQueryRequest(
    string? Query = null,
    DateOnly? PickupDate = null,
    DateOnly? ReturnDate = null,
    string? Filters = null,
    string? Sorts = null,
    int Page = Paging.FirstPage,
    int PageSize = Paging.DefaultPageSize) : IRequestContract<CarQueryDto>
{
    public CarQueryDto ToDto() => ContractMappings.Map(this);
}
