namespace Harmonie.Application.Features.Guilds.BanMember;

public sealed record BanMemberRequest(
    string UserId,
    string? Reason = null,
    int PurgeMessagesDays = 0);
