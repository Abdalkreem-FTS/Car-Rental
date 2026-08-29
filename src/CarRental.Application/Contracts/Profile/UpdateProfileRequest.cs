namespace CarRental.Application.Contracts.Profile;

public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    DateOnly? DateOfBirth,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Country,
    string DriverLicenseNumber);
