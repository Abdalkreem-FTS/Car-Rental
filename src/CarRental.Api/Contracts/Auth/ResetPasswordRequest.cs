using CarRental.Api.Mapping;
using CarRental.Application.Dtos.Auth;

namespace CarRental.Api.Contracts.Auth;

public sealed record ResetPasswordRequest(
    string Email,
    string Token,
    string Password,
    string ConfirmPassword) : IRequestContract<ResetPasswordDto>
{
    public ResetPasswordDto ToDto() => ContractMappings.Map(this);
}
