namespace CarRental.Application.Dtos.Reservations;

public sealed record CreateReservationDto(
    Guid CarId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? PickupLocation);
