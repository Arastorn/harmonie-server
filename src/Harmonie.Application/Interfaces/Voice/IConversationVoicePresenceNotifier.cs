using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Uploads;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Voice;

public interface IConversationVoicePresenceNotifier
{
    Task NotifyParticipantJoinedAsync(
        ConversationVoiceParticipantJoinedNotification notification,
        CancellationToken cancellationToken = default);

    Task NotifyParticipantLeftAsync(
        ConversationVoiceParticipantLeftNotification notification,
        CancellationToken cancellationToken = default);

    Task NotifyScreenShareStartedAsync(
        ConversationVoiceScreenShareNotification notification,
        CancellationToken cancellationToken = default);

    Task NotifyScreenShareStoppedAsync(
        ConversationVoiceScreenShareNotification notification,
        CancellationToken cancellationToken = default);
}

public sealed record ConversationVoiceParticipantJoinedNotification(
    ConversationId ConversationId,
    UserId UserId,
    string? Username,
    string? DisplayName,
    UploadedFileId? AvatarFileId,
    string? AvatarColor,
    string? AvatarIcon,
    string? AvatarBg,
    DateTime JoinedAtUtc);

public sealed record ConversationVoiceParticipantLeftNotification(
    ConversationId ConversationId,
    UserId UserId,
    string? Username,
    DateTime LeftAtUtc);

public sealed record ConversationVoiceScreenShareNotification(
    ConversationId ConversationId,
    UserId UserId,
    string? Username,
    DateTime TimestampUtc);
