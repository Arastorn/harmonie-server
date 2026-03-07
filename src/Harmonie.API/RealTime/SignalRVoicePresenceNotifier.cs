using Harmonie.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Harmonie.API.RealTime;

public sealed class SignalRVoicePresenceNotifier : IVoicePresenceNotifier
{
    private readonly IHubContext<RealtimeHub> _hubContext;

    public SignalRVoicePresenceNotifier(IHubContext<RealtimeHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyParticipantJoinedAsync(
        VoiceParticipantJoinedNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var payload = new VoiceParticipantJoinedEvent(
            GuildId: notification.GuildId.ToString(),
            ChannelId: notification.ChannelId.ToString(),
            UserId: notification.UserId.ToString(),
            ParticipantName: notification.ParticipantName,
            JoinedAtUtc: notification.JoinedAtUtc);

        await _hubContext.Clients
            .Group(RealtimeHub.GetGuildGroupName(notification.GuildId))
            .SendAsync("VoiceParticipantJoined", payload, cancellationToken);
    }

    public async Task NotifyParticipantLeftAsync(
        VoiceParticipantLeftNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var payload = new VoiceParticipantLeftEvent(
            GuildId: notification.GuildId.ToString(),
            ChannelId: notification.ChannelId.ToString(),
            UserId: notification.UserId.ToString(),
            ParticipantName: notification.ParticipantName,
            LeftAtUtc: notification.LeftAtUtc);

        await _hubContext.Clients
            .Group(RealtimeHub.GetGuildGroupName(notification.GuildId))
            .SendAsync("VoiceParticipantLeft", payload, cancellationToken);
    }
}

public sealed record VoiceParticipantJoinedEvent(
    string GuildId,
    string ChannelId,
    string UserId,
    string ParticipantName,
    DateTime JoinedAtUtc);

public sealed record VoiceParticipantLeftEvent(
    string GuildId,
    string ChannelId,
    string UserId,
    string ParticipantName,
    DateTime LeftAtUtc);
