namespace Harmonie.Application.Features.Auth.Login;

/// <summary>
/// Response for successful login with authentication tokens
/// </summary>
public sealed record LoginResponse(
    string UserId,
    string Email,
    string Username,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
