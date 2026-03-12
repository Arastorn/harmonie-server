using Dapper;
using Harmonie.Application.Interfaces;
using Harmonie.Domain.Entities;
using Harmonie.Domain.ValueObjects;

namespace Harmonie.Infrastructure.Persistence;

public sealed class GuildInviteRepository : IGuildInviteRepository
{
    private readonly DbSession _dbSession;

    public GuildInviteRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task AddAsync(GuildInvite invite, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO guild_invites (
                               id,
                               code,
                               guild_id,
                               creator_id,
                               max_uses,
                               uses_count,
                               expires_at_utc,
                               created_at_utc)
                           VALUES (
                               @Id,
                               @Code,
                               @GuildId,
                               @CreatorId,
                               @MaxUses,
                               @UsesCount,
                               @ExpiresAtUtc,
                               @CreatedAtUtc)
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                Id = invite.Id.Value,
                invite.Code,
                GuildId = invite.GuildId.Value,
                CreatorId = invite.CreatorId.Value,
                invite.MaxUses,
                invite.UsesCount,
                invite.ExpiresAtUtc,
                invite.CreatedAtUtc
            },
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public async Task<InvitePreview?> GetPreviewByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT gi.code              AS "Code",
                                  g.name               AS "GuildName",
                                  g.icon_file_id       AS "GuildIconFileId",
                                  g.icon_color         AS "GuildIconColor",
                                  g.icon_name          AS "GuildIconName",
                                  g.icon_bg            AS "GuildIconBg",
                                  gi.uses_count        AS "UsesCount",
                                  gi.max_uses          AS "MaxUses",
                                  gi.expires_at_utc    AS "ExpiresAtUtc",
                                  (SELECT COUNT(*)::int FROM guild_members gm WHERE gm.guild_id = g.id) AS "MemberCount"
                           FROM guild_invites gi
                           JOIN guilds g ON g.id = gi.guild_id
                           WHERE gi.code = @Code
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { Code = code },
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<InvitePreviewRow>(command);
        if (row is null)
            return null;

        return new InvitePreview(
            row.Code,
            row.GuildName,
            row.GuildIconFileId.HasValue ? UploadedFileId.From(row.GuildIconFileId.Value) : null,
            row.GuildIconColor,
            row.GuildIconName,
            row.GuildIconBg,
            row.MemberCount,
            row.UsesCount,
            row.MaxUses,
            row.ExpiresAtUtc);
    }

    private sealed class InvitePreviewRow
    {
        public string Code { get; init; } = string.Empty;
        public string GuildName { get; init; } = string.Empty;
        public Guid? GuildIconFileId { get; init; }
        public string? GuildIconColor { get; init; }
        public string? GuildIconName { get; init; }
        public string? GuildIconBg { get; init; }
        public int MemberCount { get; init; }
        public int UsesCount { get; init; }
        public int? MaxUses { get; init; }
        public DateTime? ExpiresAtUtc { get; init; }
    }
}
