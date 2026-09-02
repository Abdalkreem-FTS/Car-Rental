using CarRental.Application.Common;
using CarRental.Domain.Enums;

namespace CarRental.Application.Dtos.Reservations;

public sealed record ReservationQueryDto(
    ReservationScope Scope = ReservationScope.All,
    int Page = Paging.FirstPage,
    int PageSize = Paging.DefaultPageSize);
