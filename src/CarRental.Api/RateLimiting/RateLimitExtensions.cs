using System.Globalization;
using System.Threading.RateLimiting;
using CarRental.Api.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CarRental.Api.RateLimiting;

public static class RateLimitExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .Validate(
                options => Positive(options.Auth) && Positive(options.Accounts) && Positive(options.Global)
                    && options.Search is { PerMinute: > 0, Burst: > 0 },
                "Every rate limit must be greater than zero.")
            .ValidateOnStart();

        services.AddRateLimiter(limiter =>
        {
            limiter.OnRejected = RejectAsync;

            limiter.AddPolicy(RateLimitPolicies.Auth, context => Window(context, options => options.Auth));
            limiter.AddPolicy(RateLimitPolicies.Accounts, context => Window(context, options => options.Accounts));
            limiter.AddPolicy(RateLimitPolicies.Search, Bucket);

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => Window(context, options => options.Global));
        });

        return services;
    }

    private static bool Positive(WindowLimit limit) => limit is { Permit: > 0, WindowMinutes: > 0 };

    private static RateLimitPartition<string> Window(HttpContext context, Func<RateLimitOptions, WindowLimit> select)
    {
        var options = Options(context);
        var key = PartitionKey(context);

        if (!options.Enabled)
        {
            return RateLimitPartition.GetNoLimiter(key);
        }

        var limit = select(options);

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit.Permit,
            Window = TimeSpan.FromMinutes(limit.WindowMinutes),
            QueueLimit = 0,
        });
    }

    private static RateLimitPartition<string> Bucket(HttpContext context)
    {
        var options = Options(context);
        var key = PartitionKey(context);

        if (!options.Enabled)
        {
            return RateLimitPartition.GetNoLimiter(key);
        }

        return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = options.Search.Burst,
            TokensPerPeriod = options.Search.PerMinute,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }

    private static RateLimitOptions Options(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;

    private static string PartitionKey(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true
            ? $"user:{context.User.GetUserId()}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

    private static ValueTask RejectAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        return new ValueTask(Results.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Too Many Requests",
                detail: "You have made too many requests. Please wait a moment and try again.",
                extensions: new Dictionary<string, object?> { ["errorCode"] = "rate_limit_exceeded" })
            .ExecuteAsync(context.HttpContext));
    }
}
