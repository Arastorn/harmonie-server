using Harmonie.Domain.Entities.Guilds;
using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Uploads;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Guilds;

public interface IGuildBanRepository
{
    Task<bool> TryAddAsync(
        GuildBan ban,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        GuildId guildId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        GuildId guildId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GuildBanWithUser>> GetByGuildIdAsync(
        GuildId guildId,
        CancellationToken cancellationToken = default);
}

public sealed record GuildBanWithUser(
    UserId UserId,
    Username Username,
    string? DisplayName,
    UploadedFileId? AvatarFileId,
    string? AvatarColor,
    string? AvatarIcon,
    string? AvatarBg,
    string? Reason,
    UserId BannedBy,
    DateTime CreatedAtUtc);
