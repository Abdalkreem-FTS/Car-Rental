using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CarRental.Api.Errors;

namespace CarRental.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId) ? userId : throw new UnauthenticatedException();
    }
}
