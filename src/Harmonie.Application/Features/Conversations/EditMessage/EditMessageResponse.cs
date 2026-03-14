using Harmonie.Application.Common;

namespace Harmonie.Application.Features.Conversations.EditMessage;

public sealed record EditMessageResponse(
    string MessageId,
    string ConversationId,
    string AuthorUserId,
    string Content,
    IReadOnlyList<MessageAttachmentDto> Attachments,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
