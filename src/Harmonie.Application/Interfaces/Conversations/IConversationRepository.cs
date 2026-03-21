using Harmonie.Domain.Entities.Conversations;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Conversations;

public sealed record ConversationGetOrCreateResult(Conversation Conversation, bool WasCreated);

public sealed record UserConversationSummary(
    ConversationId ConversationId,
    UserId OtherParticipantUserId,
    Username OtherParticipantUsername,
    DateTime CreatedAtUtc);

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(
        ConversationId conversationId,
        CancellationToken cancellationToken = default);

    Task<ConversationGetOrCreateResult> GetOrCreateAsync(
        UserId firstUserId,
        UserId secondUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserConversationSummary>> GetUserConversationsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
