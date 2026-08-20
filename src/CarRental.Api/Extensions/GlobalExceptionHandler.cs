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
        var statusCode = exception is BadHttpRequestException badRequest
            ? badRequest.StatusCode
            : StatusCodes.Status500InternalServerError;

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

                Title = isClientError ? null : "An unexpected error occurred.",
                Detail = ExceptionDetail(exception, isClientError, environment.IsDevelopment())
            }
        });
    }

    private static string ExceptionDetail(Exception exception, bool isClientError, bool isDevelopment)
    {
        if (isDevelopment)
        {
            return exception.Message;
        }

        return isClientError
            ? "The request could not be read. Check that the body is valid JSON and that every field has the type this endpoint expects."
            : "An unexpected server error occurred.";
    }
}
