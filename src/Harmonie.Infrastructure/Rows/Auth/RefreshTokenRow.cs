namespace Harmonie.Infrastructure.Rows.Auth;

public sealed record RefreshTokenRow(
    Guid Id,
    Guid UserId,
    string TokenHash,
    DateTime ExpiresAtUtc,
    DateTime? RevokedAtUtc,
    string? RevocationReason,
    Guid? ReplacedByTokenId);
