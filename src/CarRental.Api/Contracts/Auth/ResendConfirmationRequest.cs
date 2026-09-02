using CarRental.Api.Mapping;
using CarRental.Application.Dtos.Auth;

namespace CarRental.Api.Contracts.Auth;

public sealed record ResendConfirmationRequest(string Email) : IRequestContract<ResendConfirmationDto>
{
    public ResendConfirmationDto ToDto() => ContractMappings.Map(this);
}
