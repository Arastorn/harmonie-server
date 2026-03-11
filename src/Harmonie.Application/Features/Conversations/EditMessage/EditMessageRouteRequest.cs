namespace Harmonie.Application.Features.Conversations.EditMessage;

public sealed class EditMessageRouteRequest
{
    public string? ConversationId { get; init; }

    public string? MessageId { get; init; }
}
