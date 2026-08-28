namespace CarRental.Api.Extensions;

public static class RefreshTokenCookie
{
    public const string Name = "cr_refresh";

    private const string Path = "/api/auth";

    public static void Write(HttpContext context, string token, DateTimeOffset expiresAt) =>
        context.Response.Cookies.Append(Name, token, Options(context, expiresAt));

    public static string? Read(HttpContext context) =>
        context.Request.Cookies.TryGetValue(Name, out var token) && !string.IsNullOrWhiteSpace(token)
            ? token
            : null;

    public static void Clear(HttpContext context) =>
        context.Response.Cookies.Delete(Name, Options(context, DateTimeOffset.UnixEpoch));

    private static CookieOptions Options(HttpContext context, DateTimeOffset expiresAt) => new()
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expiresAt,
        IsEssential = true,
    };
}
