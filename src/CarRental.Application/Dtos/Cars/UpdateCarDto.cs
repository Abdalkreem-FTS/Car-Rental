using CarRental.Domain.Enums;

namespace CarRental.Application.Dtos.Cars;

public sealed record UpdateCarDto(
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
    string? Description) : ICarDetails;
