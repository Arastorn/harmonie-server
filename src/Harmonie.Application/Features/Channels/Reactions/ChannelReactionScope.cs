using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Channels.Reactions;

public sealed class ChannelReactionScope : IReactionScope<ChannelReactionScope.Context>
{
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(5);

    public sealed record Context(
        GuildChannelId ChannelId,
        string ChannelName,
        GuildId GuildId,
        string GuildName,
        string CallerUsername,
        string CallerDisplayName) : ScopeContext;

    private readonly GuildChannelId _channelId;
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IReactionNotifier _reactionNotifier;
    private readonly ILogger<ChannelReactionScope> _logger;

    public ChannelReactionScope(
        GuildChannelId channelId,
        IGuildChannelRepository guildChannelRepository,
        IReactionNotifier reactionNotifier,
        ILogger<ChannelReactionScope> logger)
    {
        _channelId = channelId;
        _guildChannelRepository = guildChannelRepository;
        _reactionNotifier = reactionNotifier;
        _logger = logger;
    }

    public async Task<AuthorizationResult<Context>> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var result = await ChannelScopeAuthorizer.AuthorizeAsync(_guildChannelRepository, _channelId, caller, ct);
        if (result is ChannelAuthResult.Denied denied)
            return new AuthorizationResult<Context>.Denied(denied.Error);

        var access = ((ChannelAuthResult.Authorized)result).Context;
        return new AuthorizationResult<Context>.Authorized(new Context(
            _channelId,
            access.Channel.Name,
            access.Channel.GuildId,
            access.GuildName ?? string.Empty,
            access.CallerUsername ?? string.Empty,
            access.CallerDisplayName ?? string.Empty));
    }

    public async Task NotifyReactionAddedAsync(
        Context context, MessageId messageId, UserId userId, string emoji, CancellationToken ct)
    {
        var notification = new ChannelReactionAddedNotification(
            messageId, context.ChannelId, context.ChannelName,
            context.GuildId, context.GuildName,
            userId, context.CallerUsername, context.CallerDisplayName, emoji);

        await BestEffortNotificationHelper.TryNotifyAsync(
            token => _reactionNotifier.NotifyReactionAddedToChannelAsync(notification, token),
            NotificationTimeout, _logger,
            "AddChannelReaction notification failed (best-effort). MessageId={MessageId}, ChannelId={ChannelId}",
            notification.MessageId, notification.ChannelId);
    }

    public async Task NotifyReactionRemovedAsync(
        Context context, MessageId messageId, UserId userId, string emoji, CancellationToken ct)
    {
        var notification = new ChannelReactionRemovedNotification(
            messageId, context.ChannelId, context.ChannelName,
            context.GuildId, context.GuildName,
            userId, context.CallerUsername, context.CallerDisplayName, emoji);

        await BestEffortNotificationHelper.TryNotifyAsync(
            token => _reactionNotifier.NotifyReactionRemovedFromChannelAsync(notification, token),
            NotificationTimeout, _logger,
            "RemoveChannelReaction notification failed (best-effort). MessageId={MessageId}, ChannelId={ChannelId}",
            notification.MessageId, notification.ChannelId);
    }
}
