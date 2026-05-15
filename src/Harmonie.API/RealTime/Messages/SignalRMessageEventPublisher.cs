using Harmonie.API.RealTime.Common;
using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Messages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Harmonie.API.RealTime.Messages;

public sealed class SignalRMessageEventPublisher : IMessageEventPublisher
{
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(5);

    private readonly IHubContext<RealtimeHub, IRealtimeClient> _hubContext;
    private readonly ILogger<SignalRMessageEventPublisher> _logger;

    public SignalRMessageEventPublisher(
        IHubContext<RealtimeHub, IRealtimeClient> hubContext,
        ILogger<SignalRMessageEventPublisher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task PublishCreatedAsync(MessageCreatedEventEnvelope messageEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageEvent);

        return messageEvent.Location switch
        {
            MessageEventLocation.Channel channel => BestEffortNotificationHelper.TryNotifyAsync(
                token => _hubContext.Clients
                    .Group(RealtimeHub.GetChannelGroupName(channel.ChannelId))
                    .MessageCreated(new MessageCreatedEvent(
                        messageEvent.MessageId.Value,
                        channel.ChannelId.Value,
                        channel.ChannelName,
                        channel.GuildId.Value,
                        channel.GuildName,
                        messageEvent.AuthorUserId.Value,
                        messageEvent.AuthorUsername,
                        messageEvent.AuthorDisplayName,
                        messageEvent.Content,
                        messageEvent.Attachments,
                        messageEvent.ReplyTo,
                        messageEvent.MentionedUserIds,
                        messageEvent.CreatedAtUtc), token),
                NotificationTimeout,
                _logger,
                "MessageCreated notification failed (best-effort). MessageId={MessageId}, ChannelId={ChannelId}",
                messageEvent.MessageId,
                channel.ChannelId),

            MessageEventLocation.Conversation conversation => BestEffortNotificationHelper.TryNotifyAsync(
                token => _hubContext.Clients
                    .Group(RealtimeHub.GetConversationGroupName(conversation.ConversationId))
                    .ConversationMessageCreated(new ConversationMessageCreatedEvent(
                        messageEvent.MessageId.Value,
                        conversation.ConversationId.Value,
                        conversation.ConversationName,
                        conversation.ConversationType,
                        messageEvent.AuthorUserId.Value,
                        messageEvent.AuthorUsername,
                        messageEvent.AuthorDisplayName,
                        messageEvent.Content,
                        messageEvent.Attachments,
                        messageEvent.ReplyTo,
                        messageEvent.MentionedUserIds,
                        messageEvent.CreatedAtUtc), token),
                NotificationTimeout,
                _logger,
                "ConversationMessageCreated notification failed (best-effort). MessageId={MessageId}, ConversationId={ConversationId}",
                messageEvent.MessageId,
                conversation.ConversationId),

            _ => Task.CompletedTask
        };
    }

    public Task PublishUpdatedAsync(MessageUpdatedEventEnvelope messageEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageEvent);

        return messageEvent.Location switch
        {
            MessageEventLocation.Channel channel => BestEffortNotificationHelper.TryNotifyAsync(
                token => _hubContext.Clients
                    .Group(RealtimeHub.GetChannelGroupName(channel.ChannelId))
                    .MessageUpdated(new MessageUpdatedEvent(
                        messageEvent.MessageId.Value,
                        channel.ChannelId.Value,
                        channel.ChannelName,
                        channel.GuildId.Value,
                        channel.GuildName,
                        messageEvent.Content,
                        messageEvent.MentionedUserIds,
                        messageEvent.UpdatedAtUtc), token),
                NotificationTimeout,
                _logger,
                "MessageUpdated notification failed (best-effort). MessageId={MessageId}, ChannelId={ChannelId}",
                messageEvent.MessageId,
                channel.ChannelId),

            MessageEventLocation.Conversation conversation => BestEffortNotificationHelper.TryNotifyAsync(
                token => _hubContext.Clients
                    .Group(RealtimeHub.GetConversationGroupName(conversation.ConversationId))
                    .ConversationMessageUpdated(new ConversationMessageUpdatedEvent(
                        messageEvent.MessageId.Value,
                        conversation.ConversationId.Value,
                        conversation.ConversationName,
                        conversation.ConversationType,
                        messageEvent.Content,
                        messageEvent.MentionedUserIds,
                        messageEvent.UpdatedAtUtc), token),
                NotificationTimeout,
                _logger,
                "ConversationMessageUpdated notification failed (best-effort). MessageId={MessageId}, ConversationId={ConversationId}",
                messageEvent.MessageId,
                conversation.ConversationId),

            _ => Task.CompletedTask
        };
    }

    public Task PublishDeletedAsync(MessageDeletedEventEnvelope messageEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageEvent);

        return messageEvent.Location switch
        {
            MessageEventLocation.Channel channel => BestEffortNotificationHelper.TryNotifyAsync(
                token => _hubContext.Clients
                    .Group(RealtimeHub.GetChannelGroupName(channel.ChannelId))
                    .MessageDeleted(new MessageDeletedEvent(
                        messageEvent.MessageId.Value,
                        channel.ChannelId.Value,
                        channel.ChannelName,
                        channel.GuildId.Value,
                        channel.GuildName), token),
                NotificationTimeout,
                _logger,
                "MessageDeleted notification failed (best-effort). MessageId={MessageId}, ChannelId={ChannelId}",
                messageEvent.MessageId,
                channel.ChannelId),

            MessageEventLocation.Conversation conversation => BestEffortNotificationHelper.TryNotifyAsync(
                token => _hubContext.Clients
                    .Group(RealtimeHub.GetConversationGroupName(conversation.ConversationId))
                    .ConversationMessageDeleted(new ConversationMessageDeletedEvent(
                        messageEvent.MessageId.Value,
                        conversation.ConversationId.Value,
                        conversation.ConversationName,
                        conversation.ConversationType), token),
                NotificationTimeout,
                _logger,
                "ConversationMessageDeleted notification failed (best-effort). MessageId={MessageId}, ConversationId={ConversationId}",
                messageEvent.MessageId,
                conversation.ConversationId),

            _ => Task.CompletedTask
        };
    }

    public Task PublishPreviewUpdatedAsync(MessagePreviewUpdatedEventEnvelope messageEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageEvent);

        return messageEvent.Location switch
        {
            MessageEventLocation.Channel channel => BestEffortNotificationHelper.TryNotifyAsync(
                token => _hubContext.Clients
                    .Group(RealtimeHub.GetChannelGroupName(channel.ChannelId))
                    .MessagePreviewUpdated(new MessagePreviewUpdatedEvent(
                        messageEvent.MessageId.Value,
                        channel.ChannelId.Value,
                        channel.ChannelName,
                        ConversationId: null,
                        ConversationName: null,
                        ConversationType: null,
                        channel.GuildId.Value,
                        channel.GuildName,
                        messageEvent.Previews), token),
                NotificationTimeout,
                _logger,
                "MessagePreviewUpdated notification failed (best-effort). MessageId={MessageId}, ChannelId={ChannelId}",
                messageEvent.MessageId,
                channel.ChannelId),

            MessageEventLocation.Conversation conversation => BestEffortNotificationHelper.TryNotifyAsync(
                token => _hubContext.Clients
                    .Group(RealtimeHub.GetConversationGroupName(conversation.ConversationId))
                    .MessagePreviewUpdated(new MessagePreviewUpdatedEvent(
                        messageEvent.MessageId.Value,
                        ChannelId: null,
                        ChannelName: null,
                        conversation.ConversationId.Value,
                        conversation.ConversationName,
                        conversation.ConversationType,
                        GuildId: null,
                        GuildName: null,
                        messageEvent.Previews), token),
                NotificationTimeout,
                _logger,
                "MessagePreviewUpdated notification failed (best-effort). MessageId={MessageId}, ConversationId={ConversationId}",
                messageEvent.MessageId,
                conversation.ConversationId),

            _ => Task.CompletedTask
        };
    }
}
