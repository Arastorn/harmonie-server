namespace Harmonie.Application.Features.Channels.JoinVoiceChannel;

public sealed record JoinVoiceChannelResponse(
    string Token,
    string Url,
    string RoomName,
    IReadOnlyList<JoinVoiceChannelParticipantResponse> CurrentParticipants);

public sealed record JoinVoiceChannelParticipantResponse(
    Guid UserId,
    string? Username,
    string? DisplayName,
    Guid? AvatarFileId,
    string? AvatarColor,
    string? AvatarIcon,
    string? AvatarBg);
