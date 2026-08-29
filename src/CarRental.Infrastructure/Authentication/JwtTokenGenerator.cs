using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CarRental.Infrastructure.Authentication;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider clock) : IJwtTokenGenerator
{
    public const string RoleClaimType = "role";

    private static readonly JsonWebTokenHandler Handler = new();

    private readonly JwtOptions _options = options.Value;

    private readonly SigningCredentials _credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key)),
        SecurityAlgorithms.HmacSha256);

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public TimeSpan RefreshReuseLeeway => TimeSpan.FromSeconds(_options.RefreshReuseLeewaySeconds);

    public (string Token, DateTimeOffset ExpiresAtUtc) GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var issuedAtUtc = clock.GetUtcNow();
        var expiresAtUtc = issuedAtUtc.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, Required(user.Email, user.Id)),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
        };

        claims.AddRange(roles.Select(role => new Claim(RoleClaimType, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = issuedAtUtc.UtcDateTime,
            NotBefore = issuedAtUtc.UtcDateTime,
            Expires = expiresAtUtc.UtcDateTime,
            SigningCredentials = _credentials,
        };

        return (Handler.CreateToken(descriptor), expiresAtUtc);
    }

    public string GenerateRefreshToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));

    private static string Required(string? email, Guid userId) =>
        string.IsNullOrWhiteSpace(email)
            ? throw new InvalidOperationException($"User {userId} has no email address to put in a token.")
            : email;
}
