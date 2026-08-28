namespace CarRental.Api.Extensions;

public static class SecurityHeaderExtensions
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    extension(WebApplication app)
    {
        public WebApplication UseSecurityHeaders() =>
            (WebApplication)app.Use(async (context, next) =>
            {
                context.Response.OnStarting(static state =>
                {
                    var headers = ((HttpContext)state).Response.Headers;

                    headers.ContentSecurityPolicy = ContentSecurityPolicy;
                    headers.XContentTypeOptions = "nosniff";
                    headers["Referrer-Policy"] = "no-referrer";
                    headers.XFrameOptions = "DENY";

                    return Task.CompletedTask;
                }, context);

                await next();
            });
    }
}
