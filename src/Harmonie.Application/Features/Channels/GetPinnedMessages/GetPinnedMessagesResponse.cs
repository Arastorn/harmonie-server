using Harmonie.Application.Common.Messages;

namespace Harmonie.Application.Features.Channels.GetPinnedMessages;

public sealed record GetPinnedMessagesResponse(
    Guid ChannelId,
    IReadOnlyList<GetPinnedMessagesItemResponse> Items,
    string? NextCursor);
