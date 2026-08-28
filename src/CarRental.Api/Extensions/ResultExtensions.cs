using CarRental.Application.Contracts.Auth;
using CarRental.Domain.Common;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace CarRental.Api.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess) =>
        result.Match(onSuccess, errors => errors.ToProblem());

    public static IResult ToOk<TValue>(this Result<TValue> result) =>
        result.ToHttpResult(Results.Ok);

    public static IResult ToOkWithRefreshCookie(this Result<AuthResponse> result, HttpContext context) =>
        result.ToHttpResult(auth =>
        {
            RefreshTokenCookie.Write(context, auth.RefreshToken, auth.RefreshExpiresAtUtc);

            return Results.Ok(auth);
        });

    public static IResult ToCreated<TValue>(this Result<TValue> result, Func<TValue, string> location) =>
        result.ToHttpResult(value => Results.Created(location(value), value));

    public static IResult ToNoContent<TValue>(this Result<TValue> result) =>
        result.ToHttpResult(_ => Results.NoContent());

    public static IResult ToAccepted<TValue>(this Result<TValue> result) =>
        result.ToHttpResult(_ => Results.Accepted());
}
