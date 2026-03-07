using Harmonie.Domain.Entities;
using Harmonie.Domain.ValueObjects;

namespace Harmonie.Application.Interfaces;

public sealed record ConversationGetOrCreateResult(Conversation Conversation, bool WasCreated);

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(
        ConversationId conversationId,
        CancellationToken cancellationToken = default);

    Task<ConversationGetOrCreateResult> GetOrCreateAsync(
        UserId firstUserId,
        UserId secondUserId,
        CancellationToken cancellationToken = default);
}
