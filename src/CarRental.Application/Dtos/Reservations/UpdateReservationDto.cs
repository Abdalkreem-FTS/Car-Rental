namespace CarRental.Application.Dtos.Reservations;

public sealed record UpdateReservationDto(
    DateOnly StartDate,
    DateOnly EndDate,
    string? PickupLocation);
