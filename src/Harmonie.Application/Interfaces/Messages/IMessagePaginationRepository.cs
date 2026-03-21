using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Messages;

public interface IMessagePaginationRepository
{
    Task<MessagePage> GetChannelPageAsync(
        GuildChannelId channelId,
        MessageCursor? beforeCursor,
        int limit,
        UserId callerId,
        CancellationToken cancellationToken = default);

    Task<MessagePage> GetConversationPageAsync(
        ConversationId conversationId,
        MessageCursor? cursor,
        int limit,
        UserId callerId,
        CancellationToken cancellationToken = default);
}
