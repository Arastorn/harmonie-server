namespace Harmonie.Application.Features.Auth.RefreshToken;

/// <summary>
/// Request to refresh an access token
/// </summary>
public sealed record RefreshTokenRequest(
    string RefreshToken);
