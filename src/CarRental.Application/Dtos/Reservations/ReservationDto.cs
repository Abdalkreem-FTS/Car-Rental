using CarRental.Domain.Enums;

namespace CarRental.Application.Dtos.Reservations;

public sealed record ReservationDto(
    Guid Id,
    Guid CarId,
    string CarMake,
    string CarModel,
    int CarYear,
    string? CarImageUrl,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalDays,
    decimal DailyRate,
    decimal TotalPrice,
    ReservationStatus Status,
    string? PickupLocation,
    DateTimeOffset CreatedAtUtc,
    string Version,
    decimal? PreviousTotalPrice = null);
