using Harmonie.Domain.Entities;
using Harmonie.Domain.ValueObjects;

namespace Harmonie.Application.Interfaces;

public interface IGuildBanRepository
{
    Task<bool> TryAddAsync(
        GuildBan ban,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        GuildId guildId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        GuildId guildId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
