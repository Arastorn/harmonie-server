using Dapper;
using Harmonie.Application.Interfaces;
using Harmonie.Domain.ValueObjects;
using Harmonie.Infrastructure.Dto;
using Npgsql;

namespace Harmonie.Infrastructure.Persistence;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly string _connectionString;

    public RefreshTokenRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task StoreAsync(
        UserId userId,
        string tokenHash,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO refresh_tokens (id, user_id, token_hash, created_at_utc, expires_at_utc)
            VALUES (@Id, @UserId, @TokenHash, @CreatedAtUtc, @ExpiresAtUtc)";

        await using var conn = CreateConnection();
        var cmd = new CommandDefinition(sql, new
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            TokenHash = tokenHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc
        }, cancellationToken: cancellationToken);

        await conn.ExecuteAsync(cmd);
    }

    public async Task<RefreshTokenSession?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id as ""Id"",
                   user_id as ""UserId"",
                   token_hash as ""TokenHash"",
                   expires_at_utc as ""ExpiresAtUtc"",
                   revoked_at_utc as ""RevokedAtUtc""
            FROM refresh_tokens
            WHERE token_hash = @TokenHash";

        await using var conn = CreateConnection();
        var cmd = new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken);
        var tokenRow = await conn.QueryFirstOrDefaultAsync<RefreshTokenDto>(cmd);

        if (tokenRow is null)
            return null;

        return new RefreshTokenSession(
            Id: tokenRow.Id,
            UserId: UserId.From(tokenRow.UserId),
            ExpiresAtUtc: tokenRow.ExpiresAtUtc,
            RevokedAtUtc: tokenRow.RevokedAtUtc);
    }

    public async Task<bool> RotateAsync(
        Guid tokenId,
        UserId userId,
        string newTokenHash,
        DateTime newExpiresAtUtc,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        const string revokeSql = """
                                 UPDATE refresh_tokens
                                 SET revoked_at_utc = @RevokedAtUtc
                                 WHERE id = @Id
                                   AND revoked_at_utc IS NULL
                                 """;

        const string insertSql = """
                                 INSERT INTO refresh_tokens (id, user_id, token_hash, created_at_utc, expires_at_utc)
                                 VALUES (@Id, @UserId, @TokenHash, @CreatedAtUtc, @ExpiresAtUtc)
                                 """;

        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        var revokeCmd = new CommandDefinition(
            revokeSql,
            new { Id = tokenId, RevokedAtUtc = revokedAtUtc },
            transaction: tx,
            cancellationToken: cancellationToken);

        var affectedRows = await conn.ExecuteAsync(revokeCmd);
        if (affectedRows != 1)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var insertCmd = new CommandDefinition(
            insertSql,
            new
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                TokenHash = newTokenHash,
                CreatedAtUtc = revokedAtUtc,
                ExpiresAtUtc = newExpiresAtUtc
            },
            transaction: tx,
            cancellationToken: cancellationToken);

        await conn.ExecuteAsync(insertCmd);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}
