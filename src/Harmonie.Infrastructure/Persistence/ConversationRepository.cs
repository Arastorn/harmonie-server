using Dapper;
using Harmonie.Application.Interfaces;
using Harmonie.Domain.Entities;
using Harmonie.Domain.ValueObjects;
using Harmonie.Infrastructure.Rows;

namespace Harmonie.Infrastructure.Persistence;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly DbSession _dbSession;

    public ConversationRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<Conversation?> GetByIdAsync(
        ConversationId conversationId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id             AS "Id",
                                  user1_id       AS "User1Id",
                                  user2_id       AS "User2Id",
                                  created_at_utc AS "CreatedAtUtc"
                           FROM conversations
                           WHERE id = @ConversationId
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { ConversationId = conversationId.Value },
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<ConversationRow>(command);
        return row is null ? null : MapToConversation(row);
    }

    public async Task<ConversationGetOrCreateResult> GetOrCreateAsync(
        UserId firstUserId,
        UserId secondUserId,
        CancellationToken cancellationToken = default)
    {
        if (firstUserId == secondUserId)
            throw new ArgumentException("Conversation participants must be different users.");

        var (user1Id, user2Id) = NormalizeParticipants(firstUserId, secondUserId);
        var createdConversationId = ConversationId.New();
        var createdAtUtc = DateTime.UtcNow;

        const string sql = """
                           WITH inserted AS (
                               INSERT INTO conversations (
                                   id,
                                   user1_id,
                                   user2_id,
                                   created_at_utc)
                               VALUES (
                                   @ConversationId,
                                   @User1Id,
                                   @User2Id,
                                   @CreatedAtUtc)
                               ON CONFLICT ((LEAST(user1_id, user2_id)), (GREATEST(user1_id, user2_id)))
                               DO NOTHING
                               RETURNING id             AS "Id",
                                         user1_id       AS "User1Id",
                                         user2_id       AS "User2Id",
                                         created_at_utc AS "CreatedAtUtc",
                                         TRUE           AS "WasCreated"
                           )
                           SELECT inserted."Id",
                                  inserted."User1Id",
                                  inserted."User2Id",
                                  inserted."CreatedAtUtc",
                                  inserted."WasCreated"
                           FROM inserted
                           UNION ALL
                           SELECT c.id             AS "Id",
                                  c.user1_id       AS "User1Id",
                                  c.user2_id       AS "User2Id",
                                  c.created_at_utc AS "CreatedAtUtc",
                                  FALSE            AS "WasCreated"
                           FROM conversations c
                           WHERE LEAST(c.user1_id, c.user2_id) = LEAST(@User1Id, @User2Id)
                             AND GREATEST(c.user1_id, c.user2_id) = GREATEST(@User1Id, @User2Id)
                           LIMIT 1
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                ConversationId = createdConversationId.Value,
                User1Id = user1Id.Value,
                User2Id = user2Id.Value,
                CreatedAtUtc = createdAtUtc
            },
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        var row = await connection.QueryFirstAsync<ConversationGetOrCreateRow>(command);
        return new ConversationGetOrCreateResult(MapToConversation(row), row.WasCreated);
    }

    private static Conversation MapToConversation(ConversationRow row)
        => Conversation.Rehydrate(
            ConversationId.From(row.Id),
            UserId.From(row.User1Id),
            UserId.From(row.User2Id),
            row.CreatedAtUtc);

    private static Conversation MapToConversation(ConversationGetOrCreateRow row)
        => Conversation.Rehydrate(
            ConversationId.From(row.Id),
            UserId.From(row.User1Id),
            UserId.From(row.User2Id),
            row.CreatedAtUtc);

    private static (UserId User1Id, UserId User2Id) NormalizeParticipants(UserId firstUserId, UserId secondUserId)
        => firstUserId.Value.CompareTo(secondUserId.Value) <= 0
            ? (firstUserId, secondUserId)
            : (secondUserId, firstUserId);

    private sealed class ConversationGetOrCreateRow
    {
        public Guid Id { get; init; }
        public Guid User1Id { get; init; }
        public Guid User2Id { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public bool WasCreated { get; init; }
    }
}
