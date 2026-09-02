using CarRental.Api.Mapping;
using CarRental.Application.Common;
using CarRental.Application.Dtos.Reservations;
using CarRental.Domain.Enums;

namespace CarRental.Api.Contracts.Reservations;

public sealed record ReservationQuery(
    ReservationScope Scope = ReservationScope.All,
    int Page = Paging.FirstPage,
    int PageSize = Paging.DefaultPageSize) : IRequestContract<ReservationQueryDto>
{
    public ReservationQueryDto ToDto() => ContractMappings.Map(this);
}
