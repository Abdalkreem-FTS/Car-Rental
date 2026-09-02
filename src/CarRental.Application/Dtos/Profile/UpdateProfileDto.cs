namespace CarRental.Application.Dtos.Profile;

public sealed record UpdateProfileDto(
    string FirstName,
    string LastName,
    string PhoneNumber,
    DateOnly? DateOfBirth,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Country,
    string DriverLicenseNumber);
