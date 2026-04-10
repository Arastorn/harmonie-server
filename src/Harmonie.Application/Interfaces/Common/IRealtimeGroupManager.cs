using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Common;

public interface IRealtimeGroupManager
{
    Task SubscribeConnectionAsync(
        UserId userId,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task AddUserToGuildGroupsAsync(
        UserId userId,
        GuildId guildId,
        CancellationToken cancellationToken = default);

    Task RemoveUserFromGuildGroupsAsync(
        UserId userId,
        GuildId guildId,
        CancellationToken cancellationToken = default);

    Task AddUserToChannelGroupAsync(
        UserId userId,
        GuildChannelId channelId,
        CancellationToken cancellationToken = default);

    Task AddAllGuildMembersToChannelGroupAsync(
        GuildId guildId,
        GuildChannelId channelId,
        CancellationToken cancellationToken = default);

    Task AddUserToConversationGroupAsync(
        UserId userId,
        ConversationId conversationId,
        CancellationToken cancellationToken = default);

    Task RemoveUserFromConversationGroupAsync(
        UserId userId,
        ConversationId conversationId,
        CancellationToken cancellationToken = default);
}
