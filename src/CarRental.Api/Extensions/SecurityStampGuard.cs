using CarRental.Application.Abstractions;
using CarRental.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace CarRental.Api.Extensions;

public static class SecurityStampGuard
{
    public static async Task RejectStaleTokensAsync(TokenValidatedContext context)
    {
        if (context.Principal is not { } principal || SubjectOf(principal) is not { } userId)
        {
            return;
        }

        var presented = principal.FindFirst(JwtTokenGenerator.SecurityStampClaimType)?.Value;

        if (string.IsNullOrEmpty(presented))
        {
            context.Fail("The token carries no security stamp.");

            return;
        }

        var services = context.HttpContext.RequestServices;

        var current = await services
            .GetRequiredService<IUserAccountStore>()
            .CurrentSecurityStampAsync(userId, context.HttpContext.RequestAborted);

        if (string.Equals(current, presented, StringComparison.Ordinal))
        {
            return;
        }

        services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(SecurityStampGuard))
            .LogInformation(
                "Refused an access token for {UserId}: the credentials it was issued against have since changed.",
                userId);

        context.Fail("The credentials this token was issued against have changed.");
    }

    private static Guid? SubjectOf(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
