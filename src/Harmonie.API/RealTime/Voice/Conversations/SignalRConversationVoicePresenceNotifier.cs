using Harmonie.API.RealTime.Common;
using Harmonie.Application.Interfaces.Voice;
using Harmonie.Domain.ValueObjects.Conversations;
using Microsoft.AspNetCore.SignalR;

namespace Harmonie.API.RealTime.Voice.Conversations;

public sealed class SignalRConversationVoicePresenceNotifier : IConversationVoicePresenceNotifier
{
    private readonly IHubContext<RealtimeHub, IRealtimeClient> _hubContext;

    public SignalRConversationVoicePresenceNotifier(IHubContext<RealtimeHub, IRealtimeClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyParticipantJoinedAsync(
        ConversationVoiceParticipantJoinedNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var payload = new ConversationVoiceParticipantJoinedEvent(
            ConversationId: notification.ConversationId.Value,
            UserId: notification.UserId.Value,
            Username: notification.Username,
            DisplayName: notification.DisplayName,
            AvatarFileId: notification.AvatarFileId?.Value,
            AvatarColor: notification.AvatarColor,
            AvatarIcon: notification.AvatarIcon,
            AvatarBg: notification.AvatarBg,
            JoinedAtUtc: notification.JoinedAtUtc);

        await _hubContext.Clients
            .Group(RealtimeHub.GetConversationGroupName(notification.ConversationId))
            .ConversationVoiceParticipantJoined(payload, cancellationToken);
    }

    public async Task NotifyParticipantLeftAsync(
        ConversationVoiceParticipantLeftNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var payload = new ConversationVoiceParticipantLeftEvent(
            ConversationId: notification.ConversationId.Value,
            UserId: notification.UserId.Value,
            Username: notification.Username,
            LeftAtUtc: notification.LeftAtUtc);

        await _hubContext.Clients
            .Group(RealtimeHub.GetConversationGroupName(notification.ConversationId))
            .ConversationVoiceParticipantLeft(payload, cancellationToken);
    }

    public async Task NotifyScreenShareStartedAsync(
        ConversationVoiceScreenShareNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var payload = new ConversationVoiceScreenShareEvent(
            ConversationId: notification.ConversationId.Value,
            UserId: notification.UserId.Value,
            Username: notification.Username,
            TimestampUtc: notification.TimestampUtc);

        await _hubContext.Clients
            .Group(RealtimeHub.GetConversationGroupName(notification.ConversationId))
            .ConversationVoiceScreenShareStarted(payload, cancellationToken);
    }

    public async Task NotifyScreenShareStoppedAsync(
        ConversationVoiceScreenShareNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var payload = new ConversationVoiceScreenShareEvent(
            ConversationId: notification.ConversationId.Value,
            UserId: notification.UserId.Value,
            Username: notification.Username,
            TimestampUtc: notification.TimestampUtc);

        await _hubContext.Clients
            .Group(RealtimeHub.GetConversationGroupName(notification.ConversationId))
            .ConversationVoiceScreenShareStopped(payload, cancellationToken);
    }
}

public sealed record ConversationVoiceParticipantJoinedEvent(
    Guid ConversationId,
    Guid UserId,
    string? Username,
    string? DisplayName,
    Guid? AvatarFileId,
    string? AvatarColor,
    string? AvatarIcon,
    string? AvatarBg,
    DateTime JoinedAtUtc);

public sealed record ConversationVoiceParticipantLeftEvent(
    Guid ConversationId,
    Guid UserId,
    string? Username,
    DateTime LeftAtUtc);

public sealed record ConversationVoiceScreenShareEvent(
    Guid ConversationId,
    Guid UserId,
    string? Username,
    DateTime TimestampUtc);
