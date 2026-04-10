using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Conversations;

public interface IConversationNotifier
{
    Task NotifyConversationCreatedAsync(
        ConversationCreatedNotification notification,
        CancellationToken cancellationToken = default);
}

public sealed record ConversationCreatedNotification(
    ConversationId ConversationId,
    string? Name,
    IReadOnlyList<UserId> ParticipantIds);
