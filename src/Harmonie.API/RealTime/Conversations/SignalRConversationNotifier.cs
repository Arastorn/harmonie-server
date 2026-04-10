using Harmonie.API.RealTime.Common;
using Harmonie.Application.Interfaces.Conversations;
using Microsoft.AspNetCore.SignalR;

namespace Harmonie.API.RealTime.Conversations;

public sealed class SignalRConversationNotifier : IConversationNotifier
{
    private readonly IHubContext<RealtimeHub> _hubContext;

    public SignalRConversationNotifier(IHubContext<RealtimeHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyConversationCreatedAsync(
        ConversationCreatedNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var payload = new ConversationCreatedEvent(
            ConversationId: notification.ConversationId.Value,
            Name: notification.Name,
            ParticipantIds: notification.ParticipantIds.Select(id => id.Value).ToArray());

        await _hubContext.Clients
            .Group(RealtimeHub.GetConversationGroupName(notification.ConversationId))
            .SendAsync("ConversationCreated", payload, cancellationToken);
    }
}

public sealed record ConversationCreatedEvent(
    Guid ConversationId,
    string? Name,
    IReadOnlyList<Guid> ParticipantIds);
