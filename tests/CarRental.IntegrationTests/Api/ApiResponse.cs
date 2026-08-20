using System.Net;

namespace CarRental.IntegrationTests.Api;

public class ApiResponse
{
    internal ApiResponse(HttpStatusCode statusCode, Problem? problem, string rawBody)
    {
        StatusCode = statusCode;
        Problem = problem;
        RawBody = rawBody;
    }

    public HttpStatusCode StatusCode { get; }

    public Problem? Problem { get; }

    public string RawBody { get; }

    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
}

public sealed class ApiResponse<T> : ApiResponse
{
    internal ApiResponse(HttpStatusCode statusCode, T? value, Problem? problem, string rawBody, Uri? location)
        : base(statusCode, problem, rawBody)
    {
        Value = value;
        Location = location;
    }

    public T? Value { get; }

    public Uri? Location { get; }
}
