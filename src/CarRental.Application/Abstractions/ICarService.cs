using CarRental.Application.Dtos.Cars;
using CarRental.Application.Dtos.Common;
using CarRental.Application.Dtos.Reservations;
using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface ICarService
{
    Task<Result<PagedResult<CarDto>>> SearchAsync(CarSearchDto request, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<CarDto>>> QueryAsync(CarQueryDto request, CancellationToken cancellationToken = default);

    Task<Result<CarDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<List<string>>> GetLocationsAsync(CancellationToken cancellationToken = default);

    Task<Result<CarDto>> CreateAsync(CreateCarDto request, CancellationToken cancellationToken = default);

    Task<Result<CarDto>> UpdateAsync(Guid id, UpdateCarDto request, CancellationToken cancellationToken = default);

    Task<Result<CarDto>> ReinstateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<RetireCarResultDto>> RetireAsync(Guid id, RetireCarDto request, CancellationToken cancellationToken = default);

    Task<Result<List<ReservationDto>>> GetReservationsAsync(Guid carId, CancellationToken cancellationToken = default);
}
