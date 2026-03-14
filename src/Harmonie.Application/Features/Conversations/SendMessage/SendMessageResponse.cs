using Harmonie.Application.Common;

namespace Harmonie.Application.Features.Conversations.SendMessage;

public sealed record SendMessageResponse(
    string MessageId,
    string ConversationId,
    string AuthorUserId,
    string Content,
    IReadOnlyList<MessageAttachmentDto> Attachments,
    DateTime CreatedAtUtc);
