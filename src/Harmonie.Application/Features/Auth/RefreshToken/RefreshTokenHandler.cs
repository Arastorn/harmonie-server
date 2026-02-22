using Harmonie.Application.Interfaces;
using Harmonie.Domain.Exceptions;

namespace Harmonie.Application.Features.Auth.RefreshToken;

/// <summary>
/// Handler for refresh token flow with token rotation.
/// </summary>
public sealed class RefreshTokenHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<RefreshTokenResponse> HandleAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var session = await _refreshTokenRepository.GetByTokenHashAsync(refreshTokenHash, cancellationToken);
        var nowUtc = DateTime.UtcNow;

        if (session is null || session.RevokedAtUtc is not null || session.ExpiresAtUtc <= nowUtc)
            throw new InvalidRefreshTokenException("Refresh token is invalid or expired");

        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
        if (user is null)
            throw new InvalidRefreshTokenException("Refresh token is invalid or expired");

        if (!user.IsActive)
            throw new UserInactiveException(user.Id);

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, user.Username);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtTokenService.HashRefreshToken(newRefreshToken);

        var accessTokenExpiresAt = _jwtTokenService.GetAccessTokenExpirationUtc();
        var refreshTokenExpiresAt = _jwtTokenService.GetRefreshTokenExpirationUtc();

        var rotated = await _refreshTokenRepository.RotateAsync(
            session.Id,
            user.Id,
            newRefreshTokenHash,
            refreshTokenExpiresAt,
            nowUtc,
            cancellationToken);

        if (!rotated)
            throw new InvalidRefreshTokenException("Refresh token is invalid or expired");

        return new RefreshTokenResponse(
            AccessToken: accessToken,
            RefreshToken: newRefreshToken,
            ExpiresAt: accessTokenExpiresAt);
    }
}
