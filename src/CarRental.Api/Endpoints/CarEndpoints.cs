using CarRental.Api.Extensions;
using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Cars;
using CarRental.Application.Contracts.Common;
using CarRental.Application.Contracts.Reservations;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Endpoints;

public static class CarEndpoints
{
    /// <summary>
    /// The HTTP QUERY method: safe and idempotent like GET, but it carries a request body. It has
    /// no <see cref="HttpMethods"/> constant yet because the specification is still a draft.
    /// </summary>
    private const string HttpQueryMethod = "QUERY";

    public static IEndpointRouteBuilder MapCarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cars")
            .WithTags("Cars")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/", async (
                [AsParameters] CarSearchRequest request,
                ICarService carService,
                CancellationToken cancellationToken) =>
            {
                var result = await carService.SearchAsync(request, cancellationToken);

                return result.ToOk();
            })
            .WithValidation<CarSearchRequest>()
            .RequireRateLimiting(RateLimiting.Search)
            .Produces<PagedResponse<CarResponse>>()
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Search the fleet by text, location, dates, category and price.");

        group.MapMethods("/", httpMethods: [HttpQueryMethod], async (
                [FromBody] CarQueryRequest request,
                ICarService carService,
                CancellationToken cancellationToken) =>
            {
                var result = await carService.QueryAsync(request, cancellationToken);

                return result.ToOk();
            })
            .WithValidation<CarQueryRequest>()
            .RequireRateLimiting(RateLimiting.Search)
            .Produces<PagedResponse<CarResponse>>()
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithName("QueryCars")
            .WithSummary("Search the fleet with a request body. Filtering and sorting take Sieve expressions.");

        group.MapGet("/locations", async (ICarService carService, CancellationToken cancellationToken) =>
            {
                var result = await carService.GetLocationsAsync(cancellationToken);

                return result.ToOk();
            })
            .Produces<List<string>>()
            .WithSummary("List every pickup location currently served, for the search filters.");

        group.MapGet("/{id:guid}", async (Guid id, ICarService carService, CancellationToken cancellationToken) =>
            {
                var result = await carService.GetByIdAsync(id, cancellationToken);

                return result.ToOk();
            })
            .Produces<CarResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Fetch one car.");

        var admin = app.MapGroup("/api/admin/cars")
            .WithTags("Cars (admin)")
            .RequireAuthorization(Policies.Admin)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        admin.MapPost("/", async (
                CreateCarRequest request,
                ICarService carService,
                CancellationToken cancellationToken) =>
            {
                var result = await carService.CreateAsync(request, cancellationToken);

                return result.ToCreated(car => $"/api/cars/{car.Id}");
            })
            .WithValidation<CreateCarRequest>()
            .Produces<CarResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Add a car to the fleet. Admin only.");

        admin.MapPut("/{id:guid}", async (
                Guid id,
                UpdateCarRequest request,
                ICarService carService,
                CancellationToken cancellationToken) =>
            {
                var result = await carService.UpdateAsync(id, request, cancellationToken);

                return result.ToOk();
            })
            .WithValidation<UpdateCarRequest>()
            .Produces<CarResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Update a car. Admin only.");

        admin.MapPost("/{id:guid}/reinstate", async (Guid id, ICarService carService, CancellationToken cancellationToken) =>
            {
                var result = await carService.ReinstateAsync(id, cancellationToken);

                return result.ToOk();
            })
            .Produces<CarResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Bring a retired car back into the fleet. Admin only.");

        admin.MapPost("/{id:guid}/retire", async (
                Guid id,
                RetireCarRequest? request,
                ICarService carService,
                CancellationToken cancellationToken) =>
            {
                var result = await carService.RetireAsync(id, request ?? new RetireCarRequest(), cancellationToken);

                return result.ToOk();
            })
            .Produces<RetireCarResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Take a car out of the fleet. Refused while it has confirmed bookings, unless those are cancelled with it. Admin only.");

        admin.MapGet("/{id:guid}/reservations", async (Guid id, ICarService carService, CancellationToken cancellationToken) =>
            {
                var result = await carService.GetReservationsAsync(id, cancellationToken);

                return result.ToOk();
            })
            .Produces<List<ReservationResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Every reservation ever made against a car, newest first. Admin only.");

        return app;
    }
}
