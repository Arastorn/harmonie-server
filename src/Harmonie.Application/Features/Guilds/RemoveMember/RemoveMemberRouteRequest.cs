namespace Harmonie.Application.Features.Guilds.RemoveMember;

public sealed class RemoveMemberRouteRequest
{
    public string? GuildId { get; init; }
    public string? UserId { get; init; }
}
