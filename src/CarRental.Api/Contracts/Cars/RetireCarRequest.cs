using CarRental.Api.Mapping;
using CarRental.Application.Dtos.Cars;

namespace CarRental.Api.Contracts.Cars;

public sealed record RetireCarRequest(bool CancelActiveBookings = false) : IRequestContract<RetireCarDto>
{
    public RetireCarDto ToDto() => ContractMappings.Map(this);
}
