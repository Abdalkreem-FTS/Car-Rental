using CarRental.Application.Common;

namespace CarRental.Api.Endpoints;

public static class CountryEndpoints
{
    public static IEndpointRouteBuilder MapCountryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/countries", () => Results.Ok(Countries.All))
            .WithTags("Reference")
            .AllowAnonymous()
            .Produces<IReadOnlyList<string>>()
            .WithSummary("List every country a profile may name. Anonymous, because the sign-up form needs it before there is a token.");

        return app;
    }
}
