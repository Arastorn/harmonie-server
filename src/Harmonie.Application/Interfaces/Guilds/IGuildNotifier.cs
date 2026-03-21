using Harmonie.Domain.ValueObjects.Guilds;

namespace Harmonie.Application.Interfaces.Guilds;

public interface IGuildNotifier
{
    Task NotifyGuildDeletedAsync(
        GuildDeletedNotification notification,
        CancellationToken cancellationToken = default);
}

public sealed record GuildDeletedNotification(
    GuildId GuildId);
