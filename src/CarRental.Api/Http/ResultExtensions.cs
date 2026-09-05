using CarRental.Api.Errors;
using CarRental.Api.Mapping;
using CarRental.Api.Security;
using CarRental.Application.Dtos.Auth;
using CarRental.Domain.Common;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace CarRental.Api.Http;

public static class ResultExtensions
{
    public static IResult ToHttpResult<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess) =>
        result.Match(onSuccess, errors => errors.ToProblem());

    public static IResult ToOk<TValue>(this Result<TValue> result) =>
        result.ToHttpResult(Results.Ok);

    public static IResult ToOk<TValue, TResponse>(this Result<TValue> result, Func<TValue, TResponse> toResponse) =>
        result.ToHttpResult(value => Results.Ok(toResponse(value)));

    public static IResult ToOkWithRefreshCookie(this Result<AuthDto> result, HttpContext context) =>
        result.ToHttpResult(auth =>
        {
            RefreshTokenCookie.Write(context, auth.RefreshToken, auth.RefreshExpiresAtUtc);

            return Results.Ok(auth.ToResponse());
        });

    public static IResult ToCreated<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> toResponse,
        Func<TResponse, string> location) =>
        result.ToHttpResult(value =>
        {
            var response = toResponse(value);

            return Results.Created(location(response), response);
        });

    public static IResult ToNoContent<TValue>(this Result<TValue> result) =>
        result.ToHttpResult(_ => Results.NoContent());

    public static IResult ToAccepted<TValue>(this Result<TValue> result) =>
        result.ToHttpResult(_ => Results.Accepted());
}
