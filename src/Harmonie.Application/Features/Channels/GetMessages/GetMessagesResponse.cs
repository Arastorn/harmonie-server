using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;

namespace Harmonie.Application.Features.Channels.GetMessages;

public sealed record GetMessagesResponse(
    Guid ChannelId,
    IReadOnlyList<GetMessagesItemResponse> Items,
    string? NextCursor,
    Guid? LastReadMessageId,
    DateTime? LastReadAtUtc);
