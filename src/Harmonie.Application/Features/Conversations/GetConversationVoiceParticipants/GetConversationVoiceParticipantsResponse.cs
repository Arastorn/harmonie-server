namespace Harmonie.Application.Features.Conversations.GetConversationVoiceParticipants;

public sealed record GetConversationVoiceParticipantsResponse(
    IReadOnlyList<ConversationVoiceParticipantDto> Participants);

public sealed record ConversationVoiceParticipantDto(
    Guid UserId,
    string? Username,
    string? DisplayName,
    Guid? AvatarFileId,
    string? AvatarColor,
    string? AvatarIcon,
    string? AvatarBg,
    bool IsSharingScreen = false);
