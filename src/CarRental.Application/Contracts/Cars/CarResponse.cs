using CarRental.Domain.Enums;

namespace CarRental.Application.Contracts.Cars;

public sealed record CarResponse(
    Guid Id,
    string Make,
    string Model,
    int Year,
    string PlateNumber,
    string Location,
    decimal DailyRate,
    int Seats,
    CarCategory Category,
    TransmissionType Transmission,
    FuelType Fuel,
    string? ImageUrl,
    string? Description,
    bool IsActive);
