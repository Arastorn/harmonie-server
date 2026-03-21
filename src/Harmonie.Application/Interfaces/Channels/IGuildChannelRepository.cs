using Harmonie.Domain.Entities.Guilds;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Channels;

public sealed record ChannelAccessContext(
    GuildChannel Channel,
    GuildRole? CallerRole);

public interface IGuildChannelRepository
{
    Task<GuildChannel?> GetByIdAsync(
        GuildChannelId channelId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        GuildChannel channel,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GuildChannel>> GetByGuildIdAsync(
        GuildId guildId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        GuildChannel channel,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        GuildChannelId channelId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameInGuildAsync(
        GuildId guildId,
        string name,
        GuildChannelId excludeId,
        CancellationToken cancellationToken = default);

    Task<ChannelAccessContext?> GetWithCallerRoleAsync(
        GuildChannelId channelId,
        UserId callerId,
        CancellationToken cancellationToken = default);
}
