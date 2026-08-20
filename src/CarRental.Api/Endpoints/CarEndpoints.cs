using CarRental.Api.Extensions;
using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Cars;
using CarRental.Domain.Entities;

namespace CarRental.Api.Endpoints;

public static class CarEndpoints
{
    public static IEndpointRouteBuilder MapCarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cars")
            .WithTags("Cars")
            .RequireAuthorization();

        group.MapGet("/", async (
                [AsParameters] CarSearchRequest request,
                ICarService carService,
                CancellationToken cancellationToken) =>
            {
                var result = await carService.SearchAsync(request, cancellationToken);

                return result.ToOk();
            })
            .WithValidation<CarSearchRequest>()
            .WithSummary("Search the fleet by text, location, dates, category and price.");

        group.MapGet("/locations", async (ICarService carService, CancellationToken cancellationToken) =>
            {
                var result = await carService.GetLocationsAsync(cancellationToken);

                return result.ToOk();
            })
            .WithSummary("List every pickup location currently served, for the search filters.");

        group.MapGet("/{id:guid}", async (Guid id, ICarService carService, CancellationToken cancellationToken) =>
            {
                var result = await carService.GetByIdAsync(id, cancellationToken);

                return result.ToOk();
            })
            .WithSummary("Fetch one car.");

        var admin = group.MapGroup(string.Empty).RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        admin.MapPost("/", async (
                CreateCarRequest request,
                ICarService carService,
                CancellationToken cancellationToken) =>
            {
                var result = await carService.CreateAsync(request, cancellationToken);

                return result.ToCreated(car => $"/api/cars/{car.Id}");
            })
            .WithValidation<CreateCarRequest>()
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
            .WithSummary("Update a car. Admin only.");

        admin.MapDelete("/{id:guid}", async (Guid id, ICarService carService, CancellationToken cancellationToken) =>
            {
                var result = await carService.DeleteAsync(id, cancellationToken);

                return result.ToNoContent();
            })
            .WithSummary("Retire a car from the fleet. Admin only. Existing reservations are kept.");

        return app;
    }
}
