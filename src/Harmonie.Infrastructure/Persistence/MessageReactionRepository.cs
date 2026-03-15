using Dapper;
using Harmonie.Application.Interfaces;
using Harmonie.Domain.ValueObjects;

namespace Harmonie.Infrastructure.Persistence;

public sealed class MessageReactionRepository : IMessageReactionRepository
{
    private readonly DbSession _dbSession;

    public MessageReactionRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<bool> ExistsAsync(
        MessageId messageId,
        UserId userId,
        string emoji,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT EXISTS (
                               SELECT 1
                               FROM message_reactions
                               WHERE message_id = @MessageId
                                 AND user_id    = @UserId
                                 AND emoji      = @Emoji
                           )
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                MessageId = messageId.Value,
                UserId = userId.Value,
                Emoji = emoji
            },
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task AddAsync(
        MessageId messageId,
        UserId userId,
        string emoji,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO message_reactions (message_id, user_id, emoji, created_at_utc)
                           VALUES (@MessageId, @UserId, @Emoji, @CreatedAtUtc)
                           ON CONFLICT (message_id, user_id, emoji) DO NOTHING
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                MessageId = messageId.Value,
                UserId = userId.Value,
                Emoji = emoji,
                CreatedAtUtc = createdAtUtc
            },
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public async Task RemoveAsync(
        MessageId messageId,
        UserId userId,
        string emoji,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           DELETE FROM message_reactions
                           WHERE message_id = @MessageId
                             AND user_id    = @UserId
                             AND emoji      = @Emoji
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                MessageId = messageId.Value,
                UserId = userId.Value,
                Emoji = emoji
            },
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<MessageReactionSummary>>> GetByMessageIdsAsync(
        IReadOnlyCollection<Guid> messageIds,
        UserId callerId,
        CancellationToken cancellationToken = default)
    {
        if (messageIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<MessageReactionSummary>>();

        const string sql = """
                           SELECT message_id AS "MessageId",
                                  emoji AS "Emoji",
                                  COUNT(*) AS "Count",
                                  BOOL_OR(user_id = @CallerId) AS "ReactedByCaller"
                           FROM message_reactions
                           WHERE message_id = ANY(@MessageIds)
                           GROUP BY message_id, emoji
                           ORDER BY message_id, MIN(created_at_utc)
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                MessageIds = messageIds.ToArray(),
                CallerId = callerId.Value
            },
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<ReactionSummaryRow>(command);

        return rows
            .GroupBy(row => row.MessageId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MessageReactionSummary>)group
                    .Select(row => new MessageReactionSummary(
                        row.Emoji,
                        row.Count,
                        row.ReactedByCaller))
                    .ToArray());
    }

    private sealed class ReactionSummaryRow
    {
        public Guid MessageId { get; init; }
        public string Emoji { get; init; } = string.Empty;
        public int Count { get; init; }
        public bool ReactedByCaller { get; init; }
    }
}
