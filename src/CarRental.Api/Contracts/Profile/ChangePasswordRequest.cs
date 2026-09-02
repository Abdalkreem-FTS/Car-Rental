using CarRental.Api.Mapping;
using CarRental.Application.Dtos.Profile;

namespace CarRental.Api.Contracts.Profile;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : IRequestContract<ChangePasswordDto>
{
    public ChangePasswordDto ToDto() => ContractMappings.Map(this);
}
