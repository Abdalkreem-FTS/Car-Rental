using CarRental.Api.Mapping;
using CarRental.Application.Dtos.Auth;

namespace CarRental.Api.Contracts.Auth;

public sealed record LoginRequest(string Email, string Password) : IRequestContract<LoginDto>
{
    public LoginDto ToDto() => ContractMappings.Map(this);
}
