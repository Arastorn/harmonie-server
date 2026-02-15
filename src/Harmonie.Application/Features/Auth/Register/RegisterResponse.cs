namespace Harmonie.Application.Features.Auth.Register;

/// <summary>
/// Response for successful user registration with authentication tokens
/// </summary>
public sealed record RegisterResponse(
    string UserId,
    string Email,
    string Username,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
