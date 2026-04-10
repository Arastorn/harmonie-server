using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Uploads;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Users;

public interface IUserProfileNotifier
{
    Task NotifyProfileUpdatedAsync(
        UserProfileUpdatedNotification notification,
        CancellationToken cancellationToken = default);
}

public sealed record UserProfileUpdatedNotification(
    UserId UserId,
    string? DisplayName,
    UploadedFileId? AvatarFileId,
    IReadOnlyList<GuildId> GuildIds,
    IReadOnlyList<ConversationId> ConversationIds);
