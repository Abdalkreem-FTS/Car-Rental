using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Reservation>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    void Add(Reservation reservation);
}
