using CarRental.Application.Abstractions;
using CarRental.Application.Common;
using CarRental.Application.Contracts.Cars;
using CarRental.Application.Contracts.Common;
using CarRental.Application.Contracts.Reservations;
using CarRental.Domain.Enums;
using CarRental.Application.Mapping;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using CarRental.Domain.Errors;

namespace CarRental.Application.Services;

public sealed class CarService(ICarRepository cars, IReservationRepository reservations, IUnitOfWork unitOfWork) : ICarService
{
    private const int MaxPageSize = 50;
    
    public Task<Result<PagedResponse<CarResponse>>> SearchAsync(CarSearchRequest request, CancellationToken cancellationToken = default)
    {
        var trimmedLocation = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();

        return QueryAsync((request with { Location = trimmedLocation }).ToQuery(), cancellationToken);
    }

    public async Task<Result<PagedResponse<CarResponse>>> QueryAsync(CarQueryRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = request with
        {
            Page = request.Page < Paging.FirstPage ? Paging.FirstPage : request.Page,
            PageSize = Math.Clamp(request.PageSize, Paging.MinPageSize, Paging.MaxPageSize),
            Query = string.IsNullOrWhiteSpace(request.Query) ? null : request.Query.Trim(),
        };

        var result = await cars.QueryAsync(normalized, cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        var (items, totalCount) = result.Value;

        return new PagedResponse<CarResponse>(
            [.. items.Select(car => car.ToResponse())],
            normalized.Page,
            normalized.PageSize,
            totalCount);
    }

    public async Task<Result<CarResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var car = await cars.GetByIdAsync(id, cancellationToken);

        return car is null ? CarErrors.NotFound : car.ToResponse();
    }

    public async Task<Result<List<string>>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        return await cars.GetLocationsAsync(cancellationToken);
    }

    public async Task<Result<CarResponse>> CreateAsync(CreateCarRequest request, CancellationToken cancellationToken = default)
    {
        var plate = request.PlateNumber.Trim().ToUpperInvariant();

        if (await cars.PlateExistsAsync(plate, cancellationToken: cancellationToken))
        {
            return CarErrors.PlateAlreadyInUse(plate);
        }

        var car = new Car
        {
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            PlateNumber = plate,
            Location = request.Location.Trim(),
            DailyRate = request.DailyRate,
            Seats = request.Seats,
            Category = request.Category,
            Transmission = request.Transmission,
            Fuel = request.Fuel,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        };

        cars.Add(car);

        var saved = await unitOfWork.TrySaveChangesAsync(cancellationToken);

        return saved.IsError ? saved.Errors : car.ToResponse();
    }

    public async Task<Result<CarResponse>> UpdateAsync(Guid id, UpdateCarRequest request, CancellationToken cancellationToken = default)
    {
        var car = await cars.GetIncludingRetiredAsync(id, cancellationToken);

        if (car is null)
        {
            return CarErrors.NotFound;
        }

        var plate = request.PlateNumber.Trim().ToUpperInvariant();

        if (await cars.PlateExistsAsync(plate, id, cancellationToken))
        {
            return CarErrors.PlateAlreadyInUse(plate);
        }

        car.Make = request.Make.Trim();
        car.Model = request.Model.Trim();
        car.Year = request.Year;
        car.PlateNumber = plate;
        car.Location = request.Location.Trim();
        car.DailyRate = request.DailyRate;
        car.Seats = request.Seats;
        car.Category = request.Category;
        car.Transmission = request.Transmission;
        car.Fuel = request.Fuel;
        car.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        car.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var saved = await unitOfWork.TrySaveChangesAsync(cancellationToken);

        return saved.IsError ? saved.Errors : car.ToResponse();
    }

    public async Task<Result<CarResponse>> ReinstateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var car = await cars.GetIncludingRetiredAsync(id, cancellationToken);

        if (car is null)
        {
            return CarErrors.NotFound;
        }

        car.IsActive = true;

        var saved = await unitOfWork.TrySaveChangesAsync(cancellationToken);

        return saved.IsError ? saved.Errors : car.ToResponse();
    }

    public async Task<Result<RetireCarResponse>> RetireAsync(
        Guid id,
        RetireCarRequest request,
        CancellationToken cancellationToken = default)
    {
        var car = await cars.GetIncludingRetiredAsync(id, cancellationToken);

        if (car is null)
        {
            return CarErrors.NotFound;
        }

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var unfinished = await reservations.GetUnfinishedForCarAsync(id, today, cancellationToken);

        if (unfinished.Count > 0 && !request.CancelActiveBookings)
        {
            return CarErrors.HasActiveBookings(unfinished.Count);
        }

        foreach (var reservation in unfinished)
        {
            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancelledAtUtc = DateTimeOffset.UtcNow;
        }

        car.IsActive = false;

        var saved = await unitOfWork.TrySaveChangesAsync(cancellationToken);

        return saved.IsError
            ? saved.Errors
            : new RetireCarResponse(car.ToResponse(), unfinished.Count);
    }

    public async Task<Result<List<ReservationResponse>>> GetReservationsAsync(Guid carId, CancellationToken cancellationToken = default)
    {
        if (await cars.GetIncludingRetiredAsync(carId, cancellationToken) is null)
        {
            return CarErrors.NotFound;
        }

        var items = await reservations.GetForCarAsync(carId, cancellationToken);

        return items.Select(reservation => reservation.ToResponse()).ToList();
    }
}
