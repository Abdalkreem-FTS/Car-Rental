namespace CarRental.Domain.Common;

public enum ErrorType
{
    Failure,
    Unexpected,
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden,
    Unavailable,
    Timeout,
}
