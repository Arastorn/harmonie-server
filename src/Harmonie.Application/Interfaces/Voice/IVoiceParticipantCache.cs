using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Uploads;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Voice;

public interface IVoiceParticipantCache
{
    Task AddOrUpdateAsync(GuildChannelId channelId, CachedVoiceParticipant participant, CancellationToken cancellationToken = default);

    Task RemoveAsync(GuildChannelId channelId, UserId userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CachedVoiceParticipant>> GetAsync(GuildChannelId channelId, CancellationToken cancellationToken = default);
}

public sealed record CachedVoiceParticipant(
    UserId UserId,
    string? Username,
    string? DisplayName,
    UploadedFileId? AvatarFileId,
    string? AvatarColor,
    string? AvatarIcon,
    string? AvatarBg);
