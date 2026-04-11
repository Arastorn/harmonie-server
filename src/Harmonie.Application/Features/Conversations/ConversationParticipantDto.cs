using Harmonie.Application.Features.Users;

namespace Harmonie.Application.Features.Conversations;

public sealed record ConversationParticipantDto(
    Guid UserId,
    string Username,
    string? DisplayName,
    Guid? AvatarFileId,
    AvatarAppearanceDto? Avatar);
