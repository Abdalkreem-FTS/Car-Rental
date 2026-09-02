using CarRental.Api.Mapping;
using CarRental.Application.Dtos.Cars;
using CarRental.Domain.Enums;

namespace CarRental.Api.Contracts.Cars;

public sealed record CreateCarRequest(
    string Make,
    string Model,
    int Year,
    string PlateNumber,
    string Location,
    decimal DailyRate,
    int Seats,
    CarCategory Category,
    TransmissionType Transmission,
    FuelType Fuel,
    string? ImageUrl,
    string? Description) : IRequestContract<CreateCarDto>
{
    public CreateCarDto ToDto() => ContractMappings.Map(this);
}
