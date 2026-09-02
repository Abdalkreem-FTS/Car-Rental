using CarRental.Api.Mapping;
using CarRental.Application.Dtos.Auth;

namespace CarRental.Api.Contracts.Auth;

public sealed record RefreshTokenRequest(string RefreshToken) : IRequestContract<RefreshTokenDto>
{
    public RefreshTokenDto ToDto() => ContractMappings.Map(this);
}
