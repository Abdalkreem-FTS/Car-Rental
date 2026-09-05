namespace CarRental.Application.Dtos.Profile;

public sealed record ProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateOnly? DateOfBirth,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Country,
    string DriverLicenseNumber,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<string> Roles);
