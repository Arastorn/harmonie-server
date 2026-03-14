using Harmonie.Application.Common;

namespace Harmonie.Application.Features.Channels.EditMessage;

public sealed record EditMessageResponse(
    string MessageId,
    string ChannelId,
    string AuthorUserId,
    string Content,
    IReadOnlyList<MessageAttachmentDto> Attachments,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
