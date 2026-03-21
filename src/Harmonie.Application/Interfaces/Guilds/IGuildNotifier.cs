using Harmonie.Domain.ValueObjects;

namespace Harmonie.Application.Interfaces;

public interface IGuildNotifier
{
    Task NotifyGuildDeletedAsync(
        GuildDeletedNotification notification,
        CancellationToken cancellationToken = default);
}

public sealed record GuildDeletedNotification(
    GuildId GuildId);
