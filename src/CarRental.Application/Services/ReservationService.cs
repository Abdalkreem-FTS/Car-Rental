using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Reservations;
using CarRental.Application.Mapping;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Errors;

namespace CarRental.Application.Services;

public sealed class ReservationService(
    IReservationRepository reservations,
    ICarRepository cars,
    IUserAccountStore users,
    IUnitOfWork unitOfWork) : IReservationService
{
    public async Task<Result<ReservationResponse>> CreateAsync(
        Guid userId,
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await users.HasConfirmedEmailAsync(userId, cancellationToken))
        {
            return UserErrors.EmailNotConfirmed;
        }

        var car = await cars.GetByIdAsync(request.CarId, cancellationToken);

        if (car is null)
        {
            return CarErrors.NotFound;
        }

        if (!car.IsActive)
        {
            return CarErrors.Inactive;
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        await reservations.LockForBookingAsync(car.Id, cancellationToken);

        if (!await cars.IsAvailableAsync(car.Id, request.StartDate, request.EndDate, cancellationToken: cancellationToken))
        {
            return CarErrors.Unavailable;
        }

        var reservation = new Reservation
        {
            UserId = userId,
            CarId = car.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PickupLocation = string.IsNullOrWhiteSpace(request.PickupLocation)
                ? car.Location
                : request.PickupLocation.Trim(),
            TotalPrice = 0m,
            Car = car,
        };

        reservation.TotalPrice = car.DailyRate * reservation.TotalDays;

        reservations.Add(reservation);

        var saved = await unitOfWork.TrySaveChangesAsync(cancellationToken);

        if (saved.IsError)
        {
            return saved.Errors;
        }

        await transaction.CommitAsync(cancellationToken);

        return reservation.ToResponse();
    }

    public async Task<Result<List<ReservationResponse>>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await reservations.GetForUserAsync(userId, cancellationToken);

        List<ReservationResponse> responses = [.. items.Select(reservation => reservation.ToResponse())];

        return responses;
    }

    public async Task<Result<ReservationResponse>> GetByIdAsync(Guid userId, Guid reservationId, CancellationToken cancellationToken = default)
    {
        var reservation = await reservations.GetByIdAsync(reservationId, cancellationToken);

        if (reservation is null)
        {
            return ReservationErrors.NotFound;
        }

        return reservation.UserId != userId
            ? ReservationErrors.NotYours
            : reservation.ToResponse();
    }

    public async Task<Result<ReservationResponse>> UpdateAsync(
        Guid userId,
        Guid reservationId,
        UpdateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        var reservation = await reservations.GetByIdAsync(reservationId, cancellationToken);

        if (reservation is null)
        {
            return ReservationErrors.NotFound;
        }

        if (reservation.UserId != userId)
        {
            return ReservationErrors.NotYours;
        }

        if (reservation.Status == ReservationStatus.Cancelled)
        {
            return ReservationErrors.AlreadyCancelled;
        }

        if (reservation.StartDate <= DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime))
        {
            return ReservationErrors.AlreadyStarted;
        }

        var car = reservation.Car ?? await cars.GetByIdAsync(reservation.CarId, cancellationToken);

        if (car is null)
        {
            return CarErrors.NotFound;
        }
        
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        await reservations.LockForBookingAsync(car.Id, cancellationToken);

        if (!await cars.IsAvailableAsync(car.Id, request.StartDate, request.EndDate, reservation.Id, cancellationToken))
        {
            return CarErrors.Unavailable;
        }

        reservation.StartDate = request.StartDate;
        reservation.EndDate = request.EndDate;
        reservation.PickupLocation = string.IsNullOrWhiteSpace(request.PickupLocation)
            ? car.Location
            : request.PickupLocation.Trim();

        reservation.TotalPrice = car.DailyRate * reservation.TotalDays;
        reservation.Car = car;

        var saved = await unitOfWork.TrySaveChangesAsync(cancellationToken);

        if (saved.IsError)
        {
            return saved.Errors;
        }

        await transaction.CommitAsync(cancellationToken);

        return reservation.ToResponse();
    }

    public async Task<Result<Updated>> CancelAsync(Guid userId, Guid reservationId, CancellationToken cancellationToken = default)
    {
        var reservation = await reservations.GetByIdAsync(reservationId, cancellationToken);

        if (reservation is null)
        {
            return ReservationErrors.NotFound;
        }

        if (reservation.UserId != userId)
        {
            return ReservationErrors.NotYours;
        }

        if (reservation.Status == ReservationStatus.Cancelled)
        {
            return ReservationErrors.AlreadyCancelled;
        }

        if (reservation.StartDate <= DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime))
        {
            return ReservationErrors.AlreadyStarted;
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAtUtc = DateTimeOffset.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
