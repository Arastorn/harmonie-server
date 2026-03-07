namespace Harmonie.Application.Features.Conversations.ListConversations;

public sealed record ListConversationsResponse(
    IReadOnlyList<ListConversationsItemResponse> Conversations);

public sealed record ListConversationsItemResponse(
    string ConversationId,
    string OtherParticipantUserId,
    string OtherParticipantUsername,
    DateTime CreatedAtUtc);
