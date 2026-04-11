namespace Harmonie.Application.Features.Conversations.OpenConversation;

public sealed record OpenConversationResponse(
    Guid ConversationId,
    string Type,
    IReadOnlyList<ConversationParticipantDto> Participants,
    DateTime CreatedAtUtc,
    bool Created);
