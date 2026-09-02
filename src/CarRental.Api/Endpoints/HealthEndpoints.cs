using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CarRental.Api.Endpoints;

public static class HealthEndpoints
{
    extension(WebApplication app)
    {
        public WebApplication MapHealthEndpoint()
        {
            app.MapHealthChecks("/api/health", new HealthCheckOptions { ResponseWriter = WriteReport })
                .WithMetadata(new HttpMethodMetadata([HttpMethods.Get]))
                .WithTags("Diagnostics")
                .AllowAnonymous();

            return app;
        }
    }

    private static Task WriteReport(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString().ToLowerInvariant(),
            checks = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString()),
        }));
    }
}
