using System.Security.Claims;
using CarRental.Api.Extensions;
using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Reservations;

namespace CarRental.Api.Endpoints;

public static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reservations")
            .WithTags("Reservations")
            .RequireAuthorization();

        group.MapPost("/", async (
                CreateReservationRequest request,
                ClaimsPrincipal user,
                IReservationService reservationService,
                CancellationToken cancellationToken) =>
            {
                var result = await reservationService.CreateAsync(user.GetUserId(), request, cancellationToken);

                return result.ToCreated(reservation => $"/api/reservations/{reservation.Id}");
            })
            .WithValidation<CreateReservationRequest>()
            .WithSummary("Book a car for a date range.");

        group.MapGet("/", async (
                ClaimsPrincipal user,
                IReservationService reservationService,
                CancellationToken cancellationToken) =>
            {
                var result = await reservationService.GetForUserAsync(user.GetUserId(), cancellationToken);

                return result.ToOk();
            })
            .WithSummary("List the signed-in user's reservations, newest pickup first.");

        group.MapGet("/{id:guid}", async (
                Guid id,
                ClaimsPrincipal user,
                IReservationService reservationService,
                CancellationToken cancellationToken) =>
            {
                var result = await reservationService.GetByIdAsync(user.GetUserId(), id, cancellationToken);

                return result.ToOk();
            })
            .WithSummary("Fetch one of the signed-in user's reservations.");

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateReservationRequest request,
                ClaimsPrincipal user,
                IReservationService reservationService,
                CancellationToken cancellationToken) =>
            {
                var result = await reservationService.UpdateAsync(user.GetUserId(), id, request, cancellationToken);

                return result.ToOk();
            })
            .WithValidation<UpdateReservationRequest>()
            .WithSummary("Move a reservation that has not started yet to new dates, repriced.");

        group.MapPost("/{id:guid}/cancel", async (
                Guid id,
                ClaimsPrincipal user,
                IReservationService reservationService,
                CancellationToken cancellationToken) =>
            {
                var result = await reservationService.CancelAsync(user.GetUserId(), id, cancellationToken);

                return result.ToNoContent();
            })
            .WithSummary("Cancel a reservation that has not started yet.");

        return app;
    }
}
