using Harmonie.Application.Common.Messages;

namespace Harmonie.Application.Features.Conversations.GetPinnedMessages;

public sealed record GetConversationPinnedMessagesResponse(
    Guid ConversationId,
    IReadOnlyList<GetPinnedMessagesItemResponse> Items,
    string? NextCursor);
