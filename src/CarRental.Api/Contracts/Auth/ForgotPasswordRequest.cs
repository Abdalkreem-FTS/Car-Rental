using CarRental.Api.Mapping;
using CarRental.Application.Dtos.Auth;

namespace CarRental.Api.Contracts.Auth;

public sealed record ForgotPasswordRequest(string Email) : IRequestContract<ForgotPasswordDto>
{
    public ForgotPasswordDto ToDto() => ContractMappings.Map(this);
}
