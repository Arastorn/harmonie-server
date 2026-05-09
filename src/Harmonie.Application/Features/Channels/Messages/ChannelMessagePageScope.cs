using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Channels.Messages;

public sealed class ChannelMessagePageScope : IMessagePageScope<ChannelMessagePageScope.Context>
{
    public sealed record Context : ScopeContext;

    private readonly GuildChannelId _channelId;
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IMessagePaginationRepository _paginationRepository;

    public ChannelMessagePageScope(
        GuildChannelId channelId,
        IGuildChannelRepository guildChannelRepository,
        IMessagePaginationRepository paginationRepository)
    {
        _channelId = channelId;
        _guildChannelRepository = guildChannelRepository;
        _paginationRepository = paginationRepository;
    }

    public async Task<AuthorizationResult<Context>> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var result = await ChannelScopeAuthorizer.AuthorizeAsync(_guildChannelRepository, _channelId, caller, ct);
        if (result is ChannelAuthResult.Denied denied)
            return new AuthorizationResult<Context>.Denied(denied.Error);

        return new AuthorizationResult<Context>.Authorized(new Context());
    }

    public Task<MessagePage> GetPageAsync(MessageCursor? cursor, int limit, UserId callerId, CancellationToken ct)
        => _paginationRepository.GetChannelPageAsync(_channelId, cursor, limit, callerId, ct);
}
