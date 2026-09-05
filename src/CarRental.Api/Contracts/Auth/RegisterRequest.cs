using CarRental.Api.Mapping;
using CarRental.Application.Dtos.Auth;

namespace CarRental.Api.Contracts.Auth;

public sealed record RegisterRequest(
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
    string DriverLicenseNumber) : IRequestContract<RegisterDto>
{
    public RegisterDto ToDto() => ContractMappings.Map(this);
}
