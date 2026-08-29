using CarRental.Application.Common;
using CarRental.Domain.Enums;

namespace CarRental.Application.Contracts.Reservations;

public sealed record ReservationQuery(
    ReservationScope Scope = ReservationScope.All,
    int Page = Paging.FirstPage,
    int PageSize = Paging.DefaultPageSize);
