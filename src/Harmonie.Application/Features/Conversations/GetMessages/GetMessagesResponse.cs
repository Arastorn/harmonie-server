using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;

namespace Harmonie.Application.Features.Conversations.GetMessages;

public sealed record GetMessagesResponse(
    Guid ConversationId,
    IReadOnlyList<GetMessagesItemResponse> Items,
    string? NextCursor,
    Guid? LastReadMessageId,
    DateTime? LastReadAtUtc);
