using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Conversations;

public interface IConversationNotifier
{
    Task NotifyConversationCreatedAsync(
        ConversationCreatedNotification notification,
        CancellationToken cancellationToken = default);

    Task NotifyParticipantLeftAsync(
        ConversationParticipantLeftNotification notification,
        CancellationToken cancellationToken = default);
}

public sealed record ConversationCreatedNotification(
    ConversationId ConversationId,
    string? Name,
    IReadOnlyList<UserId> ParticipantIds);

public sealed record ConversationParticipantLeftNotification(
    ConversationId ConversationId,
    UserId UserId);
