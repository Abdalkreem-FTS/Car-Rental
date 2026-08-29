using CarRental.Domain.Common;

namespace CarRental.Domain.Errors;

public static class RequestErrors
{
    public static Error Malformed => Error.BadRequest(
        "request.malformed",
        "The request could not be read. Check that the body is valid JSON and that every field has the type this endpoint expects.");

    public static Error NoSuchEndpoint => Error.NotFound(
        "request.no_such_endpoint",
        "No endpoint matches this URL.");

    public static Error MethodNotAllowed => Error.BadRequest(
        "request.method_not_allowed",
        "This endpoint does not accept that HTTP method.");

    public static Error NotAcceptable => Error.BadRequest(
        "request.not_acceptable",
        "This endpoint cannot produce any of the media types listed in the 'Accept' header.");

    public static Error UnsupportedMediaType => Error.BadRequest(
        "request.unsupported_media_type",
        "This endpoint expects a JSON body sent as 'Content-Type: application/json'.");

    public static Error Unexpected => Error.Unexpected(
        "server.unexpected",
        "An unexpected server error occurred.");
}
