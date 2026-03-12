namespace Harmonie.Application.Features.Guilds.PreviewInvite;

public sealed record PreviewInviteResponse(
    string GuildName,
    string? GuildIconFileId,
    GuildIconDto? GuildIcon,
    int MemberCount,
    int UsesCount,
    int? MaxUses,
    DateTime? ExpiresAtUtc);

public sealed record GuildIconDto(
    string? Color,
    string? Name,
    string? Bg);
