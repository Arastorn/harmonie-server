namespace Harmonie.Application.Features.Conversations.GetConversationParticipants;

public sealed record GetConversationParticipantsResponse(
    IReadOnlyList<ConversationParticipantDto> Participants);

public sealed record ConversationParticipantDto(
    Guid UserId,
    string Username,
    string? DisplayName,
    Guid? AvatarFileId,
    string? AvatarColor,
    string? AvatarIcon,
    string? AvatarBg,
    DateTime JoinedAtUtc);
