using CarRental.Application.Abstractions;
using CarRental.Application.Common;
using CarRental.Application.Contracts.Common;
using CarRental.Application.Contracts.Reservations;
using CarRental.Application.Mapping;
using CarRental.Domain;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Errors;
using Microsoft.Extensions.Logging;

namespace CarRental.Application.Services;

public sealed class ReservationService(
    IReservationRepository reservations,
    ICarRepository cars,
    IUserAccountStore users,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<ReservationService> logger) : IReservationService
{
    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    public async Task<Result<ReservationResponse>> CreateAsync(
        Guid userId,
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await users.GetRenterAsync(userId, cancellationToken) is not { } renter)
        {
            return UserErrors.NotFound;
        }

        if (!renter.EmailConfirmed)
        {
            return UserErrors.EmailNotConfirmed;
        }

        if (renter.DateOfBirth is not { } dateOfBirth)
        {
            return UserErrors.DateOfBirthMissing;
        }

        if (!RenterRules.IsOldEnough(dateOfBirth, Today))
        {
            return UserErrors.TooYoungToRent(RenterRules.MinimumAge);
        }

        var car = await cars.GetByIdAsync(request.CarId, cancellationToken);

        if (car is null)
        {
            return CarErrors.NotFound;
        }

        // Both of these are load-bearing, and neither is decorative.
        //
        // The lock and the check below settle every booking that arrives through this service:
        // the lock serialises requests for one car, and the check then reads a state nobody can
        // change until this transaction ends. That is what produces the 409.
        //
        // ck_reservations_no_overlapping_confirmed_bookings, in the migration, is what makes the
        // rule true for anything that reaches the table another way: a script, a future service,
        // a bug here. Deleting either one leaves a way to sell the same car twice.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        await reservations.LockForBookingAsync(car.Id, cancellationToken);

        if (!await cars.IsAvailableAsync(car.Id, request.StartDate, request.EndDate, cancellationToken: cancellationToken))
        {
            return CarErrors.Unavailable;
        }

        if (PickupLocationFor(car, request.PickupLocation) is not { } pickupLocation)
        {
            return CarErrors.PickupLocationNotOffered(request.PickupLocation!.Trim(), car.Location);
        }

        var reservation = new Reservation
        {
            UserId = userId,
            CarId = car.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PickupLocation = pickupLocation,
            DailyRate = car.DailyRate,
            TotalPrice = car.DailyRate * Reservation.DaysBetween(request.StartDate, request.EndDate),
            Car = car,
        };

        reservations.Add(reservation);

        var saved = await unitOfWork.TrySaveChangesAsync(cancellationToken);

        if (saved.IsError)
        {
            return saved.Errors;
        }

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Booked {ReservationId}: car {CarId} for {UserId} from {StartDate} to {EndDate} at {TotalPrice}",
            reservation.Id,
            reservation.CarId,
            userId,
            reservation.StartDate,
            reservation.EndDate,
            reservation.TotalPrice);

        return reservation.ToResponse();
    }

    public async Task<Result<PagedResponse<ReservationResponse>>> GetForUserAsync(
        Guid userId,
        ReservationQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < Paging.FirstPage ? Paging.FirstPage : query.Page;
        var pageSize = Math.Clamp(query.PageSize, Paging.MinPageSize, Paging.MaxPageSize);

        var (items, totalCount) = await reservations.GetForUserAsync(
            userId, query.Scope, Today, page, pageSize, cancellationToken);

        return new PagedResponse<ReservationResponse>(
            [.. items.Select(reservation => reservation.ToResponse())],
            page,
            pageSize,
            totalCount);
    }

    public async Task<Result<ReservationResponse>> GetByIdAsync(Guid userId, Guid reservationId, CancellationToken cancellationToken = default)
    {
        var reservation = await reservations.GetByIdAsync(reservationId, userId, cancellationToken);

        if (reservation is null)
        {
            return ReservationErrors.NotFound;
        }

        return reservation.ToResponse();
    }

    public async Task<Result<ReservationResponse>> UpdateAsync(
        Guid userId,
        Guid reservationId,
        string expectedVersion,
        UpdateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        var reservation = await reservations.GetByIdAsync(reservationId, userId, cancellationToken);

        if (reservation is null)
        {
            return ReservationErrors.NotFound;
        }

        if (reservation.Status == ReservationStatus.Cancelled)
        {
            return ReservationErrors.AlreadyCancelled;
        }

        if (reservation.StartDate <= Today)
        {
            return ReservationErrors.AlreadyStarted;
        }

        var car = reservation.Car ?? await cars.GetIncludingRetiredAsync(reservation.CarId, cancellationToken);

        if (car is null)
        {
            return CarErrors.NotFound;
        }

        if (!car.IsActive)
        {
            return CarErrors.NoLongerInTheFleet;
        }
        
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        await reservations.LockForBookingAsync(car.Id, cancellationToken);

        await reservations.ReloadAsync(reservation, cancellationToken);

        if (!string.Equals(reservation.Version.ToString(), expectedVersion, StringComparison.Ordinal))
        {
            return ReservationErrors.VersionStale;
        }

        if (!await cars.IsAvailableAsync(car.Id, request.StartDate, request.EndDate, reservation.Id, cancellationToken))
        {
            return CarErrors.Unavailable;
        }

        reservation.StartDate = request.StartDate;
        reservation.EndDate = request.EndDate;
        if (PickupLocationFor(car, request.PickupLocation) is not { } pickupLocation)
        {
            return CarErrors.PickupLocationNotOffered(request.PickupLocation!.Trim(), car.Location);
        }

        reservation.PickupLocation = pickupLocation;

        var previousTotalPrice = reservation.TotalPrice;

        reservation.TotalPrice = reservation.DailyRate * reservation.TotalDays;
        reservation.Car = car;

        var saved = await unitOfWork.TrySaveChangesAsync(cancellationToken);

        if (saved.IsError)
        {
            return saved.Errors;
        }

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Moved {ReservationId} for {UserId} to {StartDate}-{EndDate}, {PreviousTotalPrice} to {TotalPrice}",
            reservation.Id,
            userId,
            reservation.StartDate,
            reservation.EndDate,
            previousTotalPrice,
            reservation.TotalPrice);

        return reservation.ToResponse() with { PreviousTotalPrice = previousTotalPrice };
    }

    public async Task<Result<Updated>> CancelAsync(Guid userId, Guid reservationId, CancellationToken cancellationToken = default)
    {
        var reservation = await reservations.GetByIdAsync(reservationId, userId, cancellationToken);

        if (reservation is null)
        {
            return ReservationErrors.NotFound;
        }

        if (reservation.Status == ReservationStatus.Cancelled)
        {
            return ReservationErrors.AlreadyCancelled;
        }

        if (reservation.StartDate <= Today)
        {
            return ReservationErrors.AlreadyStarted;
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAtUtc = clock.GetUtcNow();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Cancelled {ReservationId} for {UserId}, releasing car {CarId} from {StartDate} to {EndDate}",
            reservation.Id,
            userId,
            reservation.CarId,
            reservation.StartDate,
            reservation.EndDate);

        return Result.Updated;
    }

    private static string? PickupLocationFor(Car car, string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return car.Location;
        }

        return string.Equals(requested.Trim(), car.Location, StringComparison.OrdinalIgnoreCase)
            ? car.Location
            : null;
    }
}
