namespace Harmonie.Application.Features.Conversations.JoinConversationVoice;

public sealed record JoinConversationVoiceResponse(
    string Token,
    string Url,
    string RoomName,
    IReadOnlyList<JoinConversationVoiceParticipantResponse> CurrentParticipants);

public sealed record JoinConversationVoiceParticipantResponse(
    Guid UserId,
    string? Username,
    string? DisplayName,
    Guid? AvatarFileId,
    string? AvatarColor,
    string? AvatarIcon,
    string? AvatarBg,
    bool IsSharingScreen = false);
