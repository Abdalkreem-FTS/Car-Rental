using CarRental.Domain.Common;
using CarRental.Domain.Errors;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace CarRental.Api.Extensions;

public static class ProblemExtensions
{
    public static IResult ToProblem(this List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Results.Problem();
        }

        return errors.All(error => error.Type == ErrorType.Validation) ? ValidationProblem(errors) : Problem(errors[0]);
    }

    public static IResult ToProblem(this Error error) => Problem(error);

    public static IResult? ProblemOrNull<TValue>(this Result<TValue> result) =>
        result.Match<IResult?>(onValue: _ => null, onError: errors => errors.ToProblem());

    public static void Customize(ProblemDetailsContext context)
    {
        Error? error = context.ProblemDetails.Status switch
        {
            StatusCodes.Status404NotFound => RequestErrors.NoSuchEndpoint,
            StatusCodes.Status405MethodNotAllowed => RequestErrors.MethodNotAllowed,
            StatusCodes.Status406NotAcceptable => RequestErrors.NotAcceptable,
            StatusCodes.Status415UnsupportedMediaType => RequestErrors.UnsupportedMediaType,
            _ => null,
        };

        if (error is not { } known)
        {
            return;
        }

        context.ProblemDetails.Detail ??= known.Description;

        // A document carries either an errors map or an errorCode, never both.
        if (context.ProblemDetails is not HttpValidationProblemDetails)
        {
            context.ProblemDetails.Extensions.TryAdd("errorCode", known.Code);
        }
    }

    private static IResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Failure => StatusCodes.Status400BadRequest,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
            ErrorType.Timeout => StatusCodes.Status504GatewayTimeout,
            ErrorType.PreconditionRequired => StatusCodes.Status428PreconditionRequired,
            ErrorType.PreconditionFailed => StatusCodes.Status412PreconditionFailed,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            statusCode: statusCode,
            title: GetTitle(error.Type),
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = error.Code
            });
    }

    private static string GetTitle(ErrorType type) => type switch
    {
        ErrorType.Conflict => "Conflict",
        ErrorType.Validation => "Validation Error",
        ErrorType.NotFound => "Not Found",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.Failure => "Bad Request",
        ErrorType.Unexpected => "Internal Server Error",
        ErrorType.Unavailable => "Service Unavailable",
        ErrorType.Timeout => "Gateway Timeout",
        ErrorType.PreconditionRequired => "Precondition Required",
        ErrorType.PreconditionFailed => "Precondition Failed",
        _ => "An error occurred"
    };

    private static IResult ValidationProblem(List<Error> errors)
    {
        var errorsDict = errors
            .GroupBy(e => e.Field ?? e.Code)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description).ToArray());

        return Results.ValidationProblem(
            errorsDict,
            statusCode: StatusCodes.Status400BadRequest,
            title: "One or more validation errors occurred.");
    }
}
