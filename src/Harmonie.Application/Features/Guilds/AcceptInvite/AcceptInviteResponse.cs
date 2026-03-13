namespace Harmonie.Application.Features.Guilds.AcceptInvite;

public sealed record AcceptInviteResponse(
    string GuildId,
    string UserId,
    string Role,
    DateTime JoinedAtUtc);
