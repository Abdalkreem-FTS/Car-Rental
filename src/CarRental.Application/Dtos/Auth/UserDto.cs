namespace CarRental.Application.Dtos.Auth;

public sealed record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    IReadOnlyList<string> Roles);
