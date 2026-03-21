using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Users;

public sealed record UserSubscriptions(
    IReadOnlyList<GuildId> GuildIds,
    IReadOnlyList<GuildChannelId> TextChannelIds,
    IReadOnlyList<ConversationId> ConversationIds);

public interface IUserSubscriptionRepository
{
    Task<UserSubscriptions> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
