using System.Security.Claims;
using CarRental.Api.Contracts.Profile;
using CarRental.Api.Filters;
using CarRental.Api.Http;
using CarRental.Api.Mapping;
using CarRental.Api.Security;
using CarRental.Application.Abstractions;
using CarRental.Application.Dtos.Profile;

namespace CarRental.Api.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile")
            .WithTags("Profile")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/", async (
                ClaimsPrincipal user,
                IProfileService profileService,
                CancellationToken cancellationToken) =>
            {
                var result = await profileService.GetAsync(user.GetUserId(), cancellationToken);

                return result.ToOk(profile => profile.ToResponse());
            })
            .Produces<ProfileResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Everything the sign-up form collected, for the account settings panel.");

        group.MapPut("/", async (
                UpdateProfileRequest request,
                ClaimsPrincipal user,
                IProfileService profileService,
                CancellationToken cancellationToken) =>
            {
                var result = await profileService.UpdateAsync(user.GetUserId(), request.ToDto(), cancellationToken);

                return result.ToOk(profile => profile.ToResponse());
            })
            .WithValidation<UpdateProfileRequest, UpdateProfileDto>()
            .Produces<ProfileResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Update personal details. The sign-in email is not changed here.");

        group.MapPut("/password", async (
                ChangePasswordRequest request,
                ClaimsPrincipal user,
                IProfileService profileService,
                CancellationToken cancellationToken) =>
            {
                var result = await profileService.ChangePasswordAsync(user.GetUserId(), request.ToDto(), cancellationToken);

                return result.ToNoContent();
            })
            .WithValidation<ChangePasswordRequest, ChangePasswordDto>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Change the password. Signs every other session out.");

        return app;
    }
}
