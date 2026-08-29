using CarRental.Domain.Enums;

namespace CarRental.Domain.Entities;

public sealed class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid UserId { get; set; }

    public required Guid CarId { get; set; }

    public required DateOnly StartDate { get; set; }

    public required DateOnly EndDate { get; set; }

    public required decimal TotalPrice { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;

    public string? PickupLocation { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CancelledAtUtc { get; set; }

    public ApplicationUser? User { get; set; }

    public Car? Car { get; set; }

    public uint Version { get; set; }

    public int TotalDays => EndDate.DayNumber - StartDate.DayNumber + 1;
}
