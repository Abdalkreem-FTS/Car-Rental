using CarRental.Domain.Common;
using CarRental.Domain.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Extensions;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // A client that hung up cannot be answered, and it is not a fault of ours. Say so at debug
        // volume rather than paging someone about a closed browser tab.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug(
                "Client cancelled {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);

            return true;
        }

        var statusCode = exception switch
        {
            UnauthenticatedException => StatusCodes.Status401Unauthorized,
            BadHttpRequestException badRequest => badRequest.StatusCode,
            _ => StatusCodes.Status500InternalServerError,
        };

        var isClientError = statusCode < StatusCodes.Status500InternalServerError;

        logger.Log(
            isClientError ? LogLevel.Information : LogLevel.Error,
            isClientError ? null : exception,
            "Request failed with {StatusCode} for {Method} {Path}: {Reason}",
            statusCode,
            httpContext.Request.Method,
            httpContext.Request.Path,
            exception.Message);

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,

                Title = exception is UnauthenticatedException
                    ? "Unauthorized"
                    : isClientError ? null : "An unexpected error occurred.",
                Detail = ExceptionDetail(exception, isClientError, environment.IsDevelopment()),
                Extensions = new Dictionary<string, object?> { ["errorCode"] = ErrorFor(exception).Code },
            }
        });
    }

    private static Error ErrorFor(Exception exception) => exception switch
    {
        UnauthenticatedException unauthenticated => unauthenticated.Error,
        BadHttpRequestException => RequestErrors.Malformed,
        _ => RequestErrors.Unexpected,
    };

    private static string ExceptionDetail(Exception exception, bool isClientError, bool isDevelopment)
    {
        if (exception is UnauthenticatedException unauthenticated)
        {
            return unauthenticated.Error.Description;
        }

        return isDevelopment ? exception.Message : ErrorFor(exception).Description;
    }
}
