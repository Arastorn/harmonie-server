namespace Harmonie.Application.Features.Conversations.OpenConversation;

public sealed record OpenConversationResponse(
    string ConversationId,
    string User1Id,
    string User2Id,
    DateTime CreatedAtUtc,
    bool Created);
