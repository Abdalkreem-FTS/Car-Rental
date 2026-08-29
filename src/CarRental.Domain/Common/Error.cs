namespace CarRental.Domain.Common;

public readonly record struct Error
{
    private Error(string code, string description, ErrorType type, string? field = null)
    {
        Code = code;
        Description = description;
        Type = type;
        Field = field;
    }

    public string Code { get; }

    public string Description { get; }

    public ErrorType Type { get; }

    public string? Field { get; }

    public static Error Failure(string code = nameof(Failure), string description = "General failure.")
        => new(code, description, ErrorType.Failure);

    public static Error Unexpected(string code = nameof(Unexpected), string description = "Unexpected error.")
        => new(code, description, ErrorType.Unexpected);

    public const string GenericValidationCode = "validation.invalid";

    public static Error Validation(string field, string description)
        => new(GenericValidationCode, description, ErrorType.Validation, field);

    public static Error Validation(string code, string field, string description)
        => new(code, description, ErrorType.Validation, field);

    public static Error Conflict(string code = nameof(Conflict), string description = "Conflict error")
        => new(code, description, ErrorType.Conflict);

    public static Error NotFound(string code = nameof(NotFound), string description = "Not found error")
        => new(code, description, ErrorType.NotFound);

    public static Error Unauthorized(string code = nameof(Unauthorized), string description = "Unauthorized error")
        => new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code = nameof(Forbidden), string description = "Forbidden error")
        => new(code, description, ErrorType.Forbidden);

    public static Error Unavailable(string code = nameof(Unavailable), string description = "Service unavailable.")
        => new(code, description, ErrorType.Unavailable);

    public static Error Timeout(string code = nameof(Timeout), string description = "The operation timed out.")
        => new(code, description, ErrorType.Timeout);

    public static Error PreconditionRequired(string code = nameof(PreconditionRequired), string description = "A precondition header is required.")
        => new(code, description, ErrorType.PreconditionRequired);

    public static Error PreconditionFailed(string code = nameof(PreconditionFailed), string description = "A precondition failed.")
        => new(code, description, ErrorType.PreconditionFailed);

    public static Error Create(int type, string code, string description, string? field = null)
        => new(code, description, (ErrorType)type, field);
}
