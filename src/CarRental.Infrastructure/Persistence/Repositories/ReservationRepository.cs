using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository(AppDbContext context) : IReservationRepository
{
    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Reservations
            .Include(reservation => reservation.Car)
            .FirstOrDefaultAsync(reservation => reservation.Id == id, cancellationToken);

    public Task<List<Reservation>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Reservations
            .AsNoTracking()
            .Include(reservation => reservation.Car)
            .Where(reservation => reservation.UserId == userId)
            .OrderByDescending(reservation => reservation.StartDate)
            .ThenByDescending(reservation => reservation.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task LockForBookingAsync(Guid carId, CancellationToken cancellationToken = default) =>
        context.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({carId.ToString()}, 0))",
            cancellationToken);

    public void Add(Reservation reservation) => context.Reservations.Add(reservation);
}
