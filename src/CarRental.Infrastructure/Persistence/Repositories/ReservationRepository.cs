using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository(AppDbContext context) : IReservationRepository
{
    public Task<Reservation?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        context.Reservations
            .IgnoreQueryFilters()
            .Include(reservation => reservation.Car)
            .FirstOrDefaultAsync(
                reservation => reservation.Id == id && reservation.UserId == userId,
                cancellationToken);

    public async Task<(List<Reservation> Items, int TotalCount)> GetForUserAsync(
        Guid userId,
        ReservationScope scope,
        DateOnly today,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Reservations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(reservation => reservation.Car)
            .Where(reservation => reservation.UserId == userId);

        query = scope switch
        {
            ReservationScope.Upcoming => query.Where(reservation =>
                reservation.Status == ReservationStatus.Confirmed && reservation.EndDate >= today),
            ReservationScope.Past => query.Where(reservation =>
                reservation.Status != ReservationStatus.Confirmed || reservation.EndDate < today),
            _ => query,
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(reservation => reservation.StartDate)
            .ThenByDescending(reservation => reservation.CreatedAtUtc)
            .ThenBy(reservation => reservation.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> HasUnfinishedForUserAsync(Guid userId, DateOnly asOf, CancellationToken cancellationToken = default) =>
        context.Reservations
            .IgnoreQueryFilters()
            .AnyAsync(
                reservation =>
                    reservation.UserId == userId &&
                    reservation.Status == ReservationStatus.Confirmed &&
                    reservation.EndDate >= asOf,
                cancellationToken);

    public Task<List<Reservation>> GetForCarAsync(Guid carId, CancellationToken cancellationToken = default) =>
        context.Reservations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(reservation => reservation.Car)
            .Where(reservation => reservation.CarId == carId)
            .OrderByDescending(reservation => reservation.StartDate)
            .ToListAsync(cancellationToken);

    public Task<List<Reservation>> GetUnfinishedForCarAsync(Guid carId, DateOnly asOf, CancellationToken cancellationToken = default) =>
        context.Reservations
            .IgnoreQueryFilters()
            .Where(reservation =>
                reservation.CarId == carId &&
                reservation.Status == ReservationStatus.Confirmed &&
                reservation.EndDate >= asOf)
            .ToListAsync(cancellationToken);

    public Task LockForBookingAsync(Guid carId, CancellationToken cancellationToken = default) =>
        context.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({carId.ToString()}, 0))",
            cancellationToken);

    public Task ReloadAsync(Reservation reservation, CancellationToken cancellationToken = default) =>
        context.Entry(reservation).ReloadAsync(cancellationToken);

    public void Add(Reservation reservation) => context.Reservations.Add(reservation);
}
