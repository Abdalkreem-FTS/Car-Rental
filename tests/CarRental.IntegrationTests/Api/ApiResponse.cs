using System.Net;

namespace CarRental.IntegrationTests.Api;

public class ApiResponse
{
    internal ApiResponse(HttpStatusCode statusCode, Problem? problem, string rawBody, string? refreshCookie = null)
    {
        StatusCode = statusCode;
        Problem = problem;
        RawBody = rawBody;
        RefreshCookie = refreshCookie;
    }

    public string? RefreshCookie { get; }

    public HttpStatusCode StatusCode { get; }

    public Problem? Problem { get; }

    public string RawBody { get; }

    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
}

public sealed class ApiResponse<T> : ApiResponse
{
    internal ApiResponse(HttpStatusCode statusCode, T? value, Problem? problem, string rawBody, Uri? location, string? refreshCookie = null)
        : base(statusCode, problem, rawBody, refreshCookie)
    {
        Value = value;
        Location = location;
    }

    public T? Value { get; }

    public Uri? Location { get; }
}
