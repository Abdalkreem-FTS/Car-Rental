using CarRental.Application.Contracts.Reservations;
using CarRental.Domain.Common;
using CarRental.Domain.Errors;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace CarRental.Api.Extensions;

public static class ETags
{
    public static bool TryReadIfMatch(HttpContext context, out string version)
    {
        version = context.Request.Headers.IfMatch.ToString().Trim().Trim('"');

        return version.Length > 0 && version != "*";
    }

    extension(Result<ReservationResponse> result)
    {
        public IResult ToOkWithETag(HttpContext context) =>
            result.ToHttpResult(reservation =>
            {
                context.Response.Headers.ETag = $"\"{reservation.Version}\"";

                return Results.Ok((object?)reservation);
            });

        public IResult ToCreatedWithETag(HttpContext context) =>
            result.ToHttpResult(reservation =>
            {
                context.Response.Headers.ETag = $"\"{reservation.Version}\"";

                return Results.Created($"/api/reservations/{reservation.Id}", (object?)reservation);
            });
    }

    public static IResult VersionRequired() => ReservationErrors.VersionRequired.ToProblem();
}
