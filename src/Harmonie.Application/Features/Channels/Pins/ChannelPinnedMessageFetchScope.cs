using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Channels.Pins;

public sealed class ChannelPinnedMessageFetchScope : IPinnedMessageFetchScope<ChannelPinnedMessageFetchScope.Context>
{
    public sealed record Context : ScopeContext;

    private readonly GuildChannelId _channelId;
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IPinnedMessageRepository _pinnedMessageRepository;

    public ChannelPinnedMessageFetchScope(
        GuildChannelId channelId,
        IGuildChannelRepository guildChannelRepository,
        IPinnedMessageRepository pinnedMessageRepository)
    {
        _channelId = channelId;
        _guildChannelRepository = guildChannelRepository;
        _pinnedMessageRepository = pinnedMessageRepository;
    }

    public async Task<AuthorizationResult<Context>> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var ctx = await _guildChannelRepository.GetWithCallerRoleAsync(_channelId, caller, ct);
        if (ctx is null)
            return Denied(ApplicationErrorCodes.Channel.NotFound, "Channel was not found");

        if (ctx.Channel.Type != GuildChannelType.Text)
            return Denied(ApplicationErrorCodes.Channel.NotText, "Pinned messages can only be listed in text channels");

        if (ctx.CallerRole is null)
            return Denied(ApplicationErrorCodes.Channel.AccessDenied, "You do not have access to this channel");

        return new AuthorizationResult<Context>.Authorized(new Context());
    }

    public Task<PinnedMessagesPage> GetPinnedPageAsync(
        UserId callerId, PinnedMessagesCursor? cursor, int limit, CancellationToken ct)
        => _pinnedMessageRepository.GetPinnedMessagesAsync(_channelId, callerId, cursor, limit, ct);

    private static AuthorizationResult<Context> Denied(string code, string detail)
        => new AuthorizationResult<Context>.Denied(new ApplicationError(code, detail));
}
