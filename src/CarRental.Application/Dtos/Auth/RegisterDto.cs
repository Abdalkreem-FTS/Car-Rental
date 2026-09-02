namespace CarRental.Application.Dtos.Auth;

public sealed record RegisterDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    string PhoneNumber,
    DateOnly? DateOfBirth,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Country,
    string DriverLicenseNumber);
