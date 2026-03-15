using Dapper;
using Harmonie.Application.Interfaces;
using Harmonie.Domain.Entities;
using Harmonie.Domain.ValueObjects;
using Npgsql;

namespace Harmonie.Infrastructure.Persistence;

public sealed class GuildBanRepository : IGuildBanRepository
{
    private readonly DbSession _dbSession;

    public GuildBanRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<bool> TryAddAsync(
        GuildBan ban,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO guild_bans (
                               guild_id,
                               user_id,
                               reason,
                               banned_by,
                               created_at_utc)
                           VALUES (
                               @GuildId,
                               @UserId,
                               @Reason,
                               @BannedBy,
                               @CreatedAtUtc)
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                GuildId = ban.GuildId.Value,
                UserId = ban.UserId.Value,
                ban.Reason,
                BannedBy = ban.BannedBy.Value,
                ban.CreatedAtUtc
            },
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        try
        {
            await connection.ExecuteAsync(command);
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }
    }

    public async Task<bool> ExistsAsync(
        GuildId guildId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT EXISTS(
                               SELECT 1
                               FROM guild_bans
                               WHERE guild_id = @GuildId
                                 AND user_id = @UserId)
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                GuildId = guildId.Value,
                UserId = userId.Value
            },
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(command);
    }
}
