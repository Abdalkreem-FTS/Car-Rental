using CarRental.Domain.Enums;

namespace CarRental.Domain.Entities;

public sealed class Car
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Make { get; set; }

    public required string Model { get; set; }

    public required int Year { get; set; }

    public required string PlateNumber { get; set; }

    public required string Location { get; set; }

    public required decimal DailyRate { get; set; }

    public required int Seats { get; set; }

    public CarCategory Category { get; set; }

    public TransmissionType Transmission { get; set; }

    public FuelType Fuel { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Reservation> Reservations { get; set; } = [];
}
