using Dapper;
using Harmonie.Application.Interfaces.Users;
using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;
using Harmonie.Infrastructure.Persistence.Common;

namespace Harmonie.Infrastructure.Persistence.Users;

public sealed class UserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly DbSession _dbSession;

    public UserSubscriptionRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<UserSubscriptions> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT 'guild'   AS "Kind",
                                  g.id      AS "Id1",
                                  NULL::uuid AS "Id2"
                           FROM guild_members gm
                           JOIN guilds g ON g.id = gm.guild_id
                           WHERE gm.user_id = @UserId

                           UNION ALL

                           SELECT 'channel' AS "Kind",
                                  gc.id     AS "Id1",
                                  NULL::uuid AS "Id2"
                           FROM guild_members gm
                           JOIN guild_channels gc ON gc.guild_id = gm.guild_id AND gc.type = 1
                           WHERE gm.user_id = @UserId

                           UNION ALL

                           SELECT 'conversation' AS "Kind",
                                  cp.conversation_id AS "Id1",
                                  NULL::uuid         AS "Id2"
                           FROM conversation_participants cp
                           WHERE cp.user_id = @UserId
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { UserId = userId.Value },
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<SubscriptionRow>(command);

        var guildIds = new List<GuildId>();
        var channelIds = new List<GuildChannelId>();
        var conversationIds = new List<ConversationId>();

        foreach (var row in rows)
        {
            switch (row.Kind)
            {
                case "guild":
                    guildIds.Add(GuildId.From(row.Id1));
                    break;
                case "channel":
                    channelIds.Add(GuildChannelId.From(row.Id1));
                    break;
                case "conversation":
                    conversationIds.Add(ConversationId.From(row.Id1));
                    break;
            }
        }

        return new UserSubscriptions(guildIds, channelIds, conversationIds);
    }

    private sealed class SubscriptionRow
    {
        public string Kind { get; init; } = string.Empty;
        public Guid Id1 { get; init; }
    }
}
