using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Uploads;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Voice;

public interface IVoiceParticipantCache
{
    Task AddOrUpdateAsync(GuildChannelId channelId, CachedVoiceParticipant participant, CancellationToken cancellationToken = default);

    Task RemoveAsync(GuildChannelId channelId, UserId userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CachedVoiceParticipant>> GetAsync(GuildChannelId channelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a ScreenShare track SID for a participant. Returns true if this is the first
    /// active screen share track for the participant (i.e., the set transitioned from empty to non-empty).
    /// </summary>
    Task<ScreenShareTrackAddResult> TryAddScreenShareTrackAsync(
        GuildChannelId channelId,
        UserId userId,
        string trackSid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a ScreenShare track SID for a participant. Returns true if this was the last
    /// active screen share track for the participant (i.e., the set transitioned from non-empty to empty).
    /// </summary>
    Task<ScreenShareTrackRemoveResult> TryRemoveScreenShareTrackAsync(
        GuildChannelId channelId,
        UserId userId,
        string trackSid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all ScreenShare track SIDs for a participant (e.g., on participant_left).
    /// Returns true if there were any active tracks to clear.
    /// </summary>
    Task<bool> ClearScreenShareTracksAsync(
        GuildChannelId channelId,
        UserId userId,
        CancellationToken cancellationToken = default);
}

public sealed record ScreenShareTrackAddResult(bool IsFirst);

public sealed record ScreenShareTrackRemoveResult(bool IsLast);

public sealed record CachedVoiceParticipant(
    UserId UserId,
    string? Username,
    string? DisplayName,
    UploadedFileId? AvatarFileId,
    string? AvatarColor,
    string? AvatarIcon,
    string? AvatarBg,
    bool IsSharingScreen = false);
