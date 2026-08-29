namespace CarRental.Application.Contracts.Reservations;

public sealed record CreateReservationRequest(
    Guid CarId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? PickupLocation);
