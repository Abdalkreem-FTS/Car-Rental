using CarRental.Api.Mapping;
using CarRental.Application.Dtos.Reservations;

namespace CarRental.Api.Contracts.Reservations;

public sealed record UpdateReservationRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    string? PickupLocation) : IRequestContract<UpdateReservationDto>
{
    public UpdateReservationDto ToDto() => ContractMappings.Map(this);
}
