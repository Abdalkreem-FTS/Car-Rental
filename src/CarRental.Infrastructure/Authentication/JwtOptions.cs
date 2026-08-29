using System.ComponentModel.DataAnnotations;

namespace CarRental.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters.")]
    public string Key { get; set; } = string.Empty;

    [Range(1, 60, ErrorMessage = "Jwt:AccessTokenMinutes must be 60 or less: an access token cannot be revoked.")]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 7;

    [Range(0, 300)]
    public int RefreshReuseLeewaySeconds { get; set; } = 5;
}
