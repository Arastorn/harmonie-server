namespace Harmonie.Application.Features.Guilds.UnbanMember;

public sealed class UnbanMemberRouteRequest
{
    public string? GuildId { get; init; }
    public string? UserId { get; init; }
}
