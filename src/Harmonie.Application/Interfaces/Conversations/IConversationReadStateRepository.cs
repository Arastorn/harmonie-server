using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Conversations;

public interface IConversationReadStateRepository
{
    Task UpsertAsync(
        UserId userId,
        ConversationId conversationId,
        MessageId lastReadMessageId,
        DateTime readAtUtc,
        CancellationToken cancellationToken = default);
}
