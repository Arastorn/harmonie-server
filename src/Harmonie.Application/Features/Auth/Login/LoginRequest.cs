namespace Harmonie.Application.Features.Auth.Login;

/// <summary>
/// Request to login with email or username
/// </summary>
public sealed record LoginRequest(
    string EmailOrUsername,
    string Password);
