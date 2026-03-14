using Harmonie.Application.Common;

namespace Harmonie.Application.Features.Channels.GetMessages;

public sealed record GetMessagesResponse(
    string ChannelId,
    IReadOnlyList<GetMessagesItemResponse> Items,
    string? NextCursor);

public sealed record GetMessagesItemResponse(
    string MessageId,
    string AuthorUserId,
    string Content,
    IReadOnlyList<MessageAttachmentDto> Attachments,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
