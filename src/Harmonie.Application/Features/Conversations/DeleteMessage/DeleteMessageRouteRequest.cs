namespace Harmonie.Application.Features.Conversations.DeleteMessage;

public sealed class DeleteMessageRouteRequest
{
    public string? ConversationId { get; init; }

    public string? MessageId { get; init; }
}
