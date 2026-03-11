namespace Harmonie.Application.Features.Conversations.EditMessage;

public sealed record EditMessageResponse(
    string MessageId,
    string ConversationId,
    string AuthorUserId,
    string Content,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
