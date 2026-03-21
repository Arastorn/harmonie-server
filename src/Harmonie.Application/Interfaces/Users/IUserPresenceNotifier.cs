using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Users;

public interface IUserPresenceNotifier
{
    Task NotifyStatusChangedAsync(
        UserPresenceChangedNotification notification,
        CancellationToken cancellationToken = default);
}

public sealed record UserPresenceChangedNotification(
    UserId UserId,
    string Status,
    IReadOnlyList<GuildId> GuildIds);
