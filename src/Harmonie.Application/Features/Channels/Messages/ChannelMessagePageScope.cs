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
        var ctx = await _guildChannelRepository.GetWithCallerRoleAsync(_channelId, caller, ct);
        if (ctx is null)
            return Denied(ApplicationErrorCodes.Channel.NotFound, "Channel was not found");

        if (ctx.Channel.Type != GuildChannelType.Text)
            return Denied(ApplicationErrorCodes.Channel.NotText, "Messages can only be read from text channels");

        if (ctx.CallerRole is null)
            return Denied(ApplicationErrorCodes.Channel.AccessDenied, "You do not have access to this channel");

        return new AuthorizationResult<Context>.Authorized(new Context());
    }

    public Task<MessagePage> GetPageAsync(MessageCursor? cursor, int limit, UserId callerId, CancellationToken ct)
        => _paginationRepository.GetChannelPageAsync(_channelId, cursor, limit, callerId, ct);

    private static AuthorizationResult<Context> Denied(string code, string detail)
        => new AuthorizationResult<Context>.Denied(new ApplicationError(code, detail));
}
