using CarRental.Application.Contracts.Cars;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions;

public interface ICarRepository
{
    Task<Car?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<(List<Car> Items, int TotalCount)>> QueryAsync(
        CarQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetLocationsAsync(CancellationToken cancellationToken = default);

    Task<bool> PlateExistsAsync(string plateNumber, Guid? excludeCarId = null, CancellationToken cancellationToken = default);

    Task<bool> IsAvailableAsync(
        Guid carId,
        DateOnly start,
        DateOnly end,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default);

    void Add(Car car);
}
