using CarRental.Application.Contracts.Cars;
using CarRental.Application.Contracts.Reservations;
using CarRental.Application.Contracts.Common;
using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface ICarService
{
    Task<Result<PagedResponse<CarResponse>>> SearchAsync(CarSearchRequest request, CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<CarResponse>>> QueryAsync(CarQueryRequest request, CancellationToken cancellationToken = default);

    Task<Result<CarResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<List<string>>> GetLocationsAsync(CancellationToken cancellationToken = default);

    Task<Result<CarResponse>> CreateAsync(CreateCarRequest request, CancellationToken cancellationToken = default);

    Task<Result<CarResponse>> UpdateAsync(Guid id, UpdateCarRequest request, CancellationToken cancellationToken = default);

    Task<Result<CarResponse>> ReinstateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<RetireCarResponse>> RetireAsync(Guid id, RetireCarRequest request, CancellationToken cancellationToken = default);

    Task<Result<List<ReservationResponse>>> GetReservationsAsync(Guid carId, CancellationToken cancellationToken = default);
}
