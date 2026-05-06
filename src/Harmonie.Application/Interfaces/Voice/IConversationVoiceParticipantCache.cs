using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Voice;

public interface IConversationVoiceParticipantCache
{
    Task AddOrUpdateAsync(ConversationId conversationId, CachedVoiceParticipant participant, CancellationToken cancellationToken = default);

    Task RemoveAsync(ConversationId conversationId, UserId userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CachedVoiceParticipant>> GetAsync(ConversationId conversationId, CancellationToken cancellationToken = default);

    Task<ScreenShareTrackAddResult> TryAddScreenShareTrackAsync(
        ConversationId conversationId,
        UserId userId,
        string trackSid,
        CancellationToken cancellationToken = default);

    Task<ScreenShareTrackRemoveResult> TryRemoveScreenShareTrackAsync(
        ConversationId conversationId,
        UserId userId,
        string trackSid,
        CancellationToken cancellationToken = default);

    Task<bool> ClearScreenShareTracksAsync(
        ConversationId conversationId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
