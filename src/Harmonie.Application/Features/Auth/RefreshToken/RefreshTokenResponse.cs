namespace Harmonie.Application.Features.Auth.RefreshToken;

/// <summary>
/// Response with new tokens
/// </summary>
public sealed record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
