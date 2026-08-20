using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Cars;
using CarRental.Application.Contracts.Common;
using CarRental.Application.Mapping;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using CarRental.Domain.Errors;

namespace CarRental.Application.Services;

public sealed class CarService(ICarRepository cars, IUnitOfWork unitOfWork) : ICarService
{
    private const int MaxPageSize = 50;

    public async Task<Result<PagedResponse<CarResponse>>> SearchAsync(CarSearchRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = request with
        {
            Page = request.Page < 1 ? 1 : request.Page,
            PageSize = Math.Clamp(request.PageSize, 1, MaxPageSize),
            Query = string.IsNullOrWhiteSpace(request.Query) ? null : request.Query.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
        };

        var (items, totalCount) = await cars.SearchAsync(normalized, cancellationToken);

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
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return car.ToResponse();
    }

    public async Task<Result<CarResponse>> UpdateAsync(Guid id, UpdateCarRequest request, CancellationToken cancellationToken = default)
    {
        var car = await cars.GetByIdAsync(id, cancellationToken);

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
        car.IsActive = request.IsActive;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return car.ToResponse();
    }

    public async Task<Result<Deleted>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var car = await cars.GetByIdAsync(id, cancellationToken);

        if (car is null)
        {
            return CarErrors.NotFound;
        }

        car.IsActive = false;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
