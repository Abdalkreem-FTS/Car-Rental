using CarRental.Application.Contracts.Reservations;
using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface IReservationService
{
    Task<Result<ReservationResponse>> CreateAsync(Guid userId, CreateReservationRequest request, CancellationToken cancellationToken = default);

    Task<Result<List<ReservationResponse>>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<ReservationResponse>> GetByIdAsync(Guid userId, Guid reservationId, CancellationToken cancellationToken = default);

    Task<Result<ReservationResponse>> UpdateAsync(
        Guid userId,
        Guid reservationId,
        UpdateReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Updated>> CancelAsync(Guid userId, Guid reservationId, CancellationToken cancellationToken = default);
}
