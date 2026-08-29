using CarRental.Domain.Entities;
using CarRental.Domain.Enums;

namespace CarRental.Application.Abstractions;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<(List<Reservation> Items, int TotalCount)> GetForUserAsync(
        Guid userId,
        ReservationScope scope,
        DateOnly today,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<List<Reservation>> GetForCarAsync(Guid carId, CancellationToken cancellationToken = default);

    Task<List<Reservation>> GetUnfinishedForCarAsync(Guid carId, DateOnly asOf, CancellationToken cancellationToken = default);

    Task LockForBookingAsync(Guid carId, CancellationToken cancellationToken = default);

    Task ReloadAsync(Reservation reservation, CancellationToken cancellationToken = default);

    void Add(Reservation reservation);
}
