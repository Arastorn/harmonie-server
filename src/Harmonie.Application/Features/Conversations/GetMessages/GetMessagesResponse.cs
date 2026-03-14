using Harmonie.Application.Common;

namespace Harmonie.Application.Features.Conversations.GetMessages;

public sealed record GetMessagesResponse(
    string ConversationId,
    IReadOnlyList<GetMessagesItemResponse> Items,
    string? NextCursor);

public sealed record GetMessagesItemResponse(
    string MessageId,
    string AuthorUserId,
    string Content,
    IReadOnlyList<MessageAttachmentDto> Attachments,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
