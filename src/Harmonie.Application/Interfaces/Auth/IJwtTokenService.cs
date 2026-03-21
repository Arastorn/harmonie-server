using Harmonie.Domain.ValueObjects;

namespace Harmonie.Application.Interfaces;

/// <summary>
/// Interface for JWT token generation and validation.
/// Manages access tokens and refresh tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate an access token for a user
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="email">User's email</param>
    /// <param name="username">User's username</param>
    /// <returns>JWT access token</returns>
    string GenerateAccessToken(UserId userId, Email email, Username username);

    /// <summary>
    /// Generate a refresh token
    /// </summary>
    /// <returns>Refresh token (cryptographically secure random string)</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Hash a refresh token before persistence.
    /// </summary>
    /// <param name="refreshToken">Refresh token in plain text</param>
    /// <returns>Deterministic hash representation of the refresh token</returns>
    string HashRefreshToken(string refreshToken);

    /// <summary>
    /// Get access token expiration timestamp in UTC.
    /// </summary>
    DateTime GetAccessTokenExpirationUtc();

    /// <summary>
    /// Get refresh token expiration timestamp in UTC.
    /// </summary>
    DateTime GetRefreshTokenExpirationUtc();

    /// <summary>
    /// Validate an access token and extract the user ID
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <param name="userId">Extracted user ID if valid</param>
    /// <returns>True if token is valid, false otherwise</returns>
    bool ValidateAccessToken(string token, out UserId? userId);
}
