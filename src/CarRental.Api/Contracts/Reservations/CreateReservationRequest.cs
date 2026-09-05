using CarRental.Api.Mapping;
using CarRental.Application.Dtos.Reservations;

namespace CarRental.Api.Contracts.Reservations;

public sealed record CreateReservationRequest(
    Guid CarId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? PickupLocation) : IRequestContract<CreateReservationDto>
{
    public CreateReservationDto ToDto() => ContractMappings.Map(this);
}
