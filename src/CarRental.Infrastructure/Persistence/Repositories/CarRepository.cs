using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Cars;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using CarRental.Domain.Errors;
using Microsoft.EntityFrameworkCore;
using Sieve.Exceptions;
using Sieve.Models;
using Sieve.Services;

namespace CarRental.Infrastructure.Persistence.Repositories;

public sealed class CarRepository(AppDbContext context, ISieveProcessor sieve) : ICarRepository
{
    public Task<Car?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Cars.FirstOrDefaultAsync(car => car.Id == id, cancellationToken);

    public Task<Car?> GetIncludingRetiredAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Cars.IgnoreQueryFilters().FirstOrDefaultAsync(car => car.Id == id, cancellationToken);

    public async Task<Result<(List<Car> Items, int TotalCount)>> QueryAsync(
        CarQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = OnTheFleet(request.Query, request.PickupDate, request.ReturnDate);
        var model = new SieveModel { Filters = request.Filters, Sorts = request.Sorts };
        
        try
        {
            query = sieve.Apply(model, query, applyFiltering: true, applySorting: false, applyPagination: false);
        }
        catch (SieveException exception)
        {
            return CarErrors.InvalidFilters(exception.Message);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        try
        {
            query = sieve.Apply(model, query, applyFiltering: false, applySorting: true, applyPagination: false);
        }
        catch (SieveException exception)
        {
            return CarErrors.InvalidSorts(exception.Message);
        }
        
        var ordered = string.IsNullOrWhiteSpace(request.Sorts)
            ? query.OrderBy(car => car.Make).ThenBy(car => car.Model).ThenBy(car => car.Id)
            : ((IOrderedQueryable<Car>)query).ThenBy(car => car.Id);

        var items = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<List<string>> GetLocationsAsync(CancellationToken cancellationToken = default) =>
        context.Cars
            .AsNoTracking()
            .Select(car => car.Location)
            .Distinct()
            .OrderBy(location => location)
            .ToListAsync(cancellationToken);

    public Task<bool> PlateExistsAsync(string plateNumber, Guid? excludeCarId = null, CancellationToken cancellationToken = default) =>
        context.Cars.IgnoreQueryFilters().AnyAsync(
            car => car.PlateNumber == plateNumber && (excludeCarId == null || car.Id != excludeCarId),
            cancellationToken);

    public async Task<bool> IsAvailableAsync(
        Guid carId,
        DateOnly start,
        DateOnly end,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default)
    {
        var hasOverlap = await context.Reservations
            .Where(Reservation.BlocksTheWindow(start, end))
            .AnyAsync(
                reservation =>
                    reservation.CarId == carId &&
                    (excludeReservationId == null || reservation.Id != excludeReservationId),
                cancellationToken);

        return !hasOverlap;
    }

    public void Add(Car car) => context.Cars.Add(car);
    
    private IQueryable<Car> OnTheFleet(string? text, DateOnly? pickupDate, DateOnly? returnDate)
    {
        var query = context.Cars.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(text))
        {
            var term = $"%{text}%";

            query = query.Where(car =>
                EF.Functions.ILike(car.Make, term) ||
                EF.Functions.ILike(car.Model, term) ||
                EF.Functions.ILike(car.Location, term));
        }

        if (pickupDate is { } pickup && returnDate is { } dropOff)
        {
            var blocked = context.Reservations
                .Where(Reservation.BlocksTheWindow(pickup, dropOff))
                .Select(reservation => reservation.CarId);

            query = query.Where(car => !blocked.Contains(car.Id));
        }

        return query;
    }
}
