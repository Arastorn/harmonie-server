using Harmonie.Domain.Entities;
using Harmonie.Domain.ValueObjects;

namespace Harmonie.Application.Interfaces;

public interface IGuildInviteRepository
{
    Task AddAsync(GuildInvite invite, CancellationToken cancellationToken = default);
    Task<InvitePreview?> GetPreviewByCodeAsync(string code, CancellationToken cancellationToken = default);
}

public sealed record InvitePreview(
    string Code,
    string GuildName,
    UploadedFileId? GuildIconFileId,
    string? GuildIconColor,
    string? GuildIconName,
    string? GuildIconBg,
    int MemberCount,
    int UsesCount,
    int? MaxUses,
    DateTime? ExpiresAtUtc);
