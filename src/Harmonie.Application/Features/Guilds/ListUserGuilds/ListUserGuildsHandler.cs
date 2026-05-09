using Harmonie.Application.Common;
using Harmonie.Application.Features.Guilds;
using Harmonie.Application.Interfaces.Guilds;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Guilds.ListUserGuilds;

public sealed class ListUserGuildsHandler : IAuthenticatedHandler<Unit, ListUserGuildsResponse>
{
    private readonly IGuildMemberRepository _guildMemberRepository;

    public ListUserGuildsHandler(
        IGuildMemberRepository guildMemberRepository)
    {
        _guildMemberRepository = guildMemberRepository;
    }

    public async Task<ApplicationResponse<ListUserGuildsResponse>> HandleAsync(
        Unit request,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await _guildMemberRepository.GetUserGuildMembershipsAsync(
            currentUserId,
            cancellationToken);

        var payload = new ListUserGuildsResponse(
            memberships.Select(membership => new ListUserGuildsItemResponse(
                    GuildId: membership.Guild.Id.Value,
                    Name: membership.Guild.Name.Value,
                    OwnerUserId: membership.Guild.OwnerUserId.Value,
                    IconFileId: membership.Guild.IconFileId?.Value,
                    Icon: membership.Guild.Icon.HasValue
                        ? new GuildIconDto(
                            membership.Guild.Icon.Color,
                            membership.Guild.Icon.Glyph,
                            membership.Guild.Icon.Bg)
                        : null,
                    Role: membership.Role.ToString(),
                    JoinedAtUtc: membership.JoinedAtUtc,
                    HasUnread: membership.HasUnread))
                .ToArray());

        return ApplicationResponse<ListUserGuildsResponse>.Ok(payload);
    }
}
