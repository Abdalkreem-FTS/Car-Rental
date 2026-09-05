using CarRental.Application.Dtos.Common;
using CarRental.Application.Dtos.Reservations;
using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface IReservationService
{
    Task<Result<ReservationDto>> CreateAsync(Guid userId, CreateReservationDto request, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<ReservationDto>>> GetForUserAsync(Guid userId, ReservationQueryDto query, CancellationToken cancellationToken = default);

    Task<Result<ReservationDto>> GetByIdAsync(Guid userId, Guid reservationId, CancellationToken cancellationToken = default);

    Task<Result<ReservationDto>> UpdateAsync(
        Guid userId,
        Guid reservationId,
        string expectedVersion,
        UpdateReservationDto request,
        CancellationToken cancellationToken = default);

    Task<Result<Updated>> CancelAsync(Guid userId, Guid reservationId, CancellationToken cancellationToken = default);
}
