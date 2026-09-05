using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CarRental.Api.Errors;
using CarRental.Api.Security;
using CarRental.Application.Abstractions;
using CarRental.Domain.Common;
using CarRental.Domain.Errors;

namespace CarRental.Api.Filters;

public sealed class IdempotencyFilter<TRequest>(IIdempotencyStore store) : IEndpointFilter
{
    public const string HeaderName = "Idempotency-Key";

    private const string ReplayHeaderName = "Idempotent-Replay";

    private const int MaxKeyLength = 128;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;

        if (!http.Request.Headers.TryGetValue(HeaderName, out var header)
            || header.ToString() is not { Length: > 0 and <= MaxKeyLength } key)
        {
            return new List<Error> { IdempotencyErrors.KeyRequired(HeaderName, MaxKeyLength) }.ToProblem();
        }

        var userId = http.User.GetUserId();
        var endpoint = $"{http.Request.Method} {http.Request.Path}";
        var hash = Hash(context.Arguments.OfType<TRequest>().FirstOrDefault());

        var claim = await store.ClaimAsync(userId, endpoint, key, hash, http.RequestAborted);

        switch (claim.Outcome)
        {
            case ClaimOutcome.KeyReused:
                return IdempotencyErrors.KeyReused.ToProblem();

            case ClaimOutcome.InProgress:
                return IdempotencyErrors.InProgress.ToProblem();

            case ClaimOutcome.Replay when claim.Existing is { StatusCode: { } status }:
                http.Response.Headers[ReplayHeaderName] = "true";

                return Results.Content(claim.Existing.ResponseBody ?? string.Empty, "application/json", statusCode: status);
        }

        object? result;

        try
        {
            result = await next(context);
        }
        catch
        {
            await store.ReleaseAsync(userId, endpoint, key, CancellationToken.None);

            throw;
        }

        var (statusCode, body) = Describe(result);

        if (statusCode is >= 200 and < 300)
        {
            await store.CompleteAsync(userId, endpoint, key, statusCode.Value, body, http.RequestAborted);
        }
        else
        {
            await store.ReleaseAsync(userId, endpoint, key, http.RequestAborted);
        }

        return result;
    }

    private static (int? StatusCode, string? Body) Describe(object? result)
    {
        var statusCode = (result as IStatusCodeHttpResult)?.StatusCode;
        var value = (result as IValueHttpResult)?.Value;

        return (statusCode, value is null ? null : JsonSerializer.Serialize(value, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private static string Hash(TRequest? request) =>
        Convert.ToHexStringLower(SHA256.HashData(
            Encoding.UTF8.GetBytes(request is null ? string.Empty : JsonSerializer.Serialize(request, JsonOptions))));
}

public static class IdempotencyFilterExtensions
{
    public static RouteHandlerBuilder WithIdempotency<TRequest>(this RouteHandlerBuilder builder) =>
        builder
            .AddEndpointFilter<IdempotencyFilter<TRequest>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
}
