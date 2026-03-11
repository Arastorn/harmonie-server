namespace Harmonie.Application.Features.Conversations.SendMessage;

public sealed record SendMessageResponse(
    string MessageId,
    string ConversationId,
    string AuthorUserId,
    string Content,
    DateTime CreatedAtUtc);
