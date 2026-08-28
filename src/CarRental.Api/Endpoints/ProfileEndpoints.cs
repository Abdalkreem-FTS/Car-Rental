using System.Security.Claims;
using CarRental.Api.Extensions;
using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Profile;

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

                return result.ToOk();
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
                var result = await profileService.UpdateAsync(user.GetUserId(), request, cancellationToken);

                return result.ToOk();
            })
            .WithValidation<UpdateProfileRequest>()
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
                var result = await profileService.ChangePasswordAsync(user.GetUserId(), request, cancellationToken);

                return result.ToNoContent();
            })
            .WithValidation<ChangePasswordRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Change the password. Signs every other session out.");

        return app;
    }
}
