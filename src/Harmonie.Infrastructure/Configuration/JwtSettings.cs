using System.ComponentModel.DataAnnotations;

namespace Harmonie.Infrastructure.Configuration;

public sealed class JwtSettings
{
    [Required(AllowEmptyStrings = false)]
    [MinLength(32)]
    public string Secret { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenExpirationMinutes { get; init; } = 15;

    [Range(1, 365)]
    public int RefreshTokenExpirationDays { get; init; } = 30;
}
