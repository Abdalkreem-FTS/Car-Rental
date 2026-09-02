using CarRental.Api.Errors;
using CarRental.Api.Mapping;
using CarRental.Application.Dtos.Reservations;
using CarRental.Domain.Common;
using CarRental.Domain.Errors;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace CarRental.Api.Http;

public static class ETags
{
    public static bool TryReadIfMatch(HttpContext context, out string version)
    {
        version = context.Request.Headers.IfMatch.ToString().Trim().Trim('"');

        return version.Length > 0 && version != "*";
    }

    extension(Result<ReservationDto> result)
    {
        public IResult ToOkWithETag(HttpContext context) =>
            result.ToHttpResult(reservation =>
            {
                context.Response.Headers.ETag = $"\"{reservation.Version}\"";

                return Results.Ok((object?)reservation.ToResponse());
            });

        public IResult ToCreatedWithETag(HttpContext context) =>
            result.ToHttpResult(reservation =>
            {
                context.Response.Headers.ETag = $"\"{reservation.Version}\"";

                return Results.Created($"/api/reservations/{reservation.Id}", (object?)reservation.ToResponse());
            });
    }

    public static IResult VersionRequired() => ReservationErrors.VersionRequired.ToProblem();
}
