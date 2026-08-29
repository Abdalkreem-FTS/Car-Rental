namespace CarRental.Application.Contracts.Reservations;

public sealed record UpdateReservationRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    string? PickupLocation);
