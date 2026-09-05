using CarRental.Domain.Rules;
using Microsoft.AspNetCore.Identity;

namespace CarRental.Domain.Entities;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    private ApplicationUser()
    {
    }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public DateOnly? DateOfBirth { get; private set; }

    public string AddressLine1 { get; private set; } = string.Empty;

    public string? AddressLine2 { get; private set; }

    public string City { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;

    public string? DriverLicenseNumber { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<RefreshToken> RefreshTokens { get; init; } = [];

    public ICollection<Reservation> Reservations { get; init; } = [];

    public string FullName => $"{FirstName} {LastName}";

    public static ApplicationUser Register(
        string email,
        string firstName,
        string lastName,
        string phoneNumber,
        DateOnly? dateOfBirth,
        string addressLine1,
        string? addressLine2,
        string city,
        string country,
        string driverLicenseNumber)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = Required(email, nameof(email)),
            UserName = Required(email, nameof(email)),
        };

        user.Describe(
            firstName,
            lastName,
            phoneNumber,
            dateOfBirth,
            addressLine1,
            addressLine2,
            city,
            country,
            driverLicenseNumber);

        return user;
    }

    public static ApplicationUser RegisterAdministrator(
        string email,
        string firstName,
        string lastName,
        string phoneNumber)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = Required(email, nameof(email)),
            UserName = Required(email, nameof(email)),
            FirstName = Required(firstName, nameof(firstName)),
            LastName = Required(lastName, nameof(lastName)),
            PhoneNumber = PhoneNumbers.Normalise(Required(phoneNumber, nameof(phoneNumber))),
        };

        return user;
    }

    public void Describe(
        string firstName,
        string lastName,
        string phoneNumber,
        DateOnly? dateOfBirth,
        string addressLine1,
        string? addressLine2,
        string city,
        string country,
        string driverLicenseNumber)
    {
        FirstName = Required(firstName, nameof(firstName));
        LastName = Required(lastName, nameof(lastName));
        PhoneNumber = PhoneNumbers.Normalise(Required(phoneNumber, nameof(phoneNumber)));
        AddressLine1 = Required(addressLine1, nameof(addressLine1));
        City = Required(city, nameof(city));
        Country = Required(country, nameof(country));
        DriverLicenseNumber = NormaliseLicence(driverLicenseNumber);

        AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim();
        DateOfBirth = dateOfBirth;
    }

    public static string NormaliseLicence(string driverLicenseNumber) =>
        Required(driverLicenseNumber, nameof(driverLicenseNumber)).ToUpperInvariant();

    private static string Required(string value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"A customer cannot be described without {field}.", field)
            : value.Trim();
}
