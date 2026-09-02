using System.Security.Claims;
using CarRental.Api.Contracts.Common;
using CarRental.Api.Contracts.Reservations;
using CarRental.Api.Filters;
using CarRental.Api.Http;
using CarRental.Api.Mapping;
using CarRental.Api.Security;
using CarRental.Application.Abstractions;
using CarRental.Application.Dtos.Reservations;

namespace CarRental.Api.Endpoints;

public static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reservations")
            .WithTags("Reservations")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/", async (
                CreateReservationRequest request,
                ClaimsPrincipal user,
                IReservationService reservationService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await reservationService.CreateAsync(user.GetUserId(), request.ToDto(), cancellationToken);

                return result.ToCreatedWithETag(context);
            })
            .WithValidation<CreateReservationRequest, CreateReservationDto>()
            .WithIdempotency<CreateReservationRequest>()
            .Produces<ReservationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Book a car for a date range.");

        group.MapGet("/", async (
                [AsParameters] ReservationQuery query,
                ClaimsPrincipal user,
                IReservationService reservationService,
                CancellationToken cancellationToken) =>
            {
                var result = await reservationService.GetForUserAsync(user.GetUserId(), query.ToDto(), cancellationToken);

                return result.ToOk(page => page.ToResponse(reservation => reservation.ToResponse()));
            })
            .WithValidation<ReservationQuery, ReservationQueryDto>()
            .Produces<PagedResponse<ReservationResponse>>()
            .WithSummary("List the signed-in user's reservations, newest pickup first.");

        group.MapGet("/{id:guid}", async (
                Guid id,
                ClaimsPrincipal user,
                IReservationService reservationService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await reservationService.GetByIdAsync(user.GetUserId(), id, cancellationToken);

                return result.ToOkWithETag(context);
            })
            .Produces<ReservationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Fetch one of the signed-in user's reservations.");

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateReservationRequest request,
                ClaimsPrincipal user,
                IReservationService reservationService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                if (!ETags.TryReadIfMatch(context, out var expectedVersion))
                {
                    return ETags.VersionRequired();
                }

                var result = await reservationService.UpdateAsync(user.GetUserId(), id, expectedVersion, request.ToDto(), cancellationToken);

                return result.ToOkWithETag(context);
            })
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithValidation<UpdateReservationRequest, UpdateReservationDto>()
            .Produces<ReservationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
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
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Cancel a reservation that has not started yet.");

        return app;
    }
}
