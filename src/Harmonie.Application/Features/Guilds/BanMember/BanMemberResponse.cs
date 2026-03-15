namespace Harmonie.Application.Features.Guilds.BanMember;

public sealed record BanMemberResponse(
    string GuildId,
    string UserId,
    string? Reason,
    string BannedBy,
    DateTime CreatedAtUtc);
