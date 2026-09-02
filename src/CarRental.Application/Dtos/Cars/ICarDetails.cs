using CarRental.Domain.Enums;

namespace CarRental.Application.Dtos.Cars;

public interface ICarDetails
{
    string Make { get; }

    string Model { get; }

    int Year { get; }

    string PlateNumber { get; }

    string Location { get; }

    decimal DailyRate { get; }

    int Seats { get; }

    CarCategory Category { get; }

    TransmissionType Transmission { get; }

    FuelType Fuel { get; }

    string? ImageUrl { get; }

    string? Description { get; }
}
