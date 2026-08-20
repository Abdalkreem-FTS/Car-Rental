using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Cars;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repositories;

public sealed class CarRepository(AppDbContext context) : ICarRepository
{
    public Task<Car?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Cars.FirstOrDefaultAsync(car => car.Id == id, cancellationToken);

    public async Task<(List<Car> Items, int TotalCount)> SearchAsync(
        CarSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Cars.AsNoTracking().Where(car => car.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var term = $"%{request.Query}%";

            query = query.Where(car =>
                EF.Functions.ILike(car.Make, term) ||
                EF.Functions.ILike(car.Model, term) ||
                EF.Functions.ILike(car.Location, term));
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            query = query.Where(car => EF.Functions.ILike(car.Location, request.Location));
        }

        if (request.Category is not null)
        {
            query = query.Where(car => car.Category == request.Category);
        }

        if (request.Transmission is not null)
        {
            query = query.Where(car => car.Transmission == request.Transmission);
        }

        if (request.Fuel is not null)
        {
            query = query.Where(car => car.Fuel == request.Fuel);
        }

        if (request.MinSeats is not null)
        {
            query = query.Where(car => car.Seats >= request.MinSeats);
        }

        if (request.MinDailyRate is not null)
        {
            query = query.Where(car => car.DailyRate >= request.MinDailyRate);
        }

        if (request.MaxDailyRate is not null)
        {
            query = query.Where(car => car.DailyRate <= request.MaxDailyRate);
        }
        
        if (request is { PickupDate: { } pickup, ReturnDate: { } dropOff })
        {
            query = query.Where(car => !car.Reservations.Any(reservation =>
                reservation.Status == ReservationStatus.Confirmed &&
                reservation.StartDate <= dropOff &&
                reservation.EndDate >= pickup));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "price_desc" => query.OrderByDescending(car => car.DailyRate).ThenBy(car => car.Id),
            "price_asc" => query.OrderBy(car => car.DailyRate).ThenBy(car => car.Id),
            "year_desc" => query.OrderByDescending(car => car.Year).ThenBy(car => car.Id),
            "seats_desc" => query.OrderByDescending(car => car.Seats).ThenBy(car => car.Id),
            _ => query.OrderBy(car => car.Make).ThenBy(car => car.Model).ThenBy(car => car.Id),
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<List<string>> GetLocationsAsync(CancellationToken cancellationToken = default) =>
        context.Cars
            .AsNoTracking()
            .Where(car => car.IsActive)
            .Select(car => car.Location)
            .Distinct()
            .OrderBy(location => location)
            .ToListAsync(cancellationToken);

    public Task<bool> PlateExistsAsync(string plateNumber, Guid? excludeCarId = null, CancellationToken cancellationToken = default) =>
        context.Cars.AnyAsync(
            car => car.PlateNumber == plateNumber && (excludeCarId == null || car.Id != excludeCarId),
            cancellationToken);

    public async Task<bool> IsAvailableAsync(
        Guid carId,
        DateOnly start,
        DateOnly end,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default)
    {
        var hasOverlap = await context.Reservations.AnyAsync(
            reservation =>
                reservation.CarId == carId &&
                reservation.Status == ReservationStatus.Confirmed &&
                (excludeReservationId == null || reservation.Id != excludeReservationId) &&
                reservation.StartDate <= end &&
                reservation.EndDate >= start,
            cancellationToken);

        return !hasOverlap;
    }

    public void Add(Car car) => context.Cars.Add(car);

    public void Remove(Car car) => context.Cars.Remove(car);
}
