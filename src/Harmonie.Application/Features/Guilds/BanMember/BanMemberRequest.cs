namespace Harmonie.Application.Features.Guilds.BanMember;

public sealed record BanMemberRequest(
    Guid UserId,
    string? Reason = null,
    int PurgeMessagesDays = 0);
