using Microsoft.AspNetCore.Identity;

namespace CarRental.Domain.Entities;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public required string AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public required string City { get; set; }

    public required string Country { get; set; }

    public required string DriverLicenseNumber { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public ICollection<Reservation> Reservations { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}";
}
