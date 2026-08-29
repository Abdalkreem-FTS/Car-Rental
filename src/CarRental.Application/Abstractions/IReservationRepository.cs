using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Reservation>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<Reservation>> GetForCarAsync(Guid carId, CancellationToken cancellationToken = default);

    Task<List<Reservation>> GetUnfinishedForCarAsync(Guid carId, DateOnly asOf, CancellationToken cancellationToken = default);

    Task LockForBookingAsync(Guid carId, CancellationToken cancellationToken = default);

    void Add(Reservation reservation);
}
