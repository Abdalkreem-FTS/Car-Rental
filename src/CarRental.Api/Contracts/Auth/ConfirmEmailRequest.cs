using CarRental.Api.Mapping;
using CarRental.Application.Dtos.Auth;

namespace CarRental.Api.Contracts.Auth;

public sealed record ConfirmEmailRequest(string Email, string Token) : IRequestContract<ConfirmEmailDto>
{
    public ConfirmEmailDto ToDto() => ContractMappings.Map(this);
}
