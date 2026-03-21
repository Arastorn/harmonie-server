using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Channels;

public interface IChannelReadStateRepository
{
    Task UpsertAsync(
        UserId userId,
        GuildChannelId channelId,
        MessageId lastReadMessageId,
        DateTime readAtUtc,
        CancellationToken cancellationToken = default);

    Task<MessageId?> GetLastReadMessageIdAsync(
        UserId userId,
        GuildChannelId channelId,
        CancellationToken cancellationToken = default);
}
