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

namespace Harmonie.Application.Features.Channels.AddReaction;

public sealed class ChannelReactionScope : IReactionScope<ChannelReactionScope.Context>
{
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(5);

    public sealed record Context(
        GuildChannelId ChannelId,
        string ChannelName,
        GuildId GuildId,
        string GuildName,
        string CallerUsername,
        string CallerDisplayName) : SendScopeContext;

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
        var ctx = await _guildChannelRepository.GetWithCallerRoleAsync(_channelId, caller, ct);
        if (ctx is null)
            return Denied(ApplicationErrorCodes.Channel.NotFound, "Channel was not found");

        if (ctx.Channel.Type != GuildChannelType.Text)
            return Denied(ApplicationErrorCodes.Channel.NotText, "Reactions can only be used in text channels");

        if (ctx.CallerRole is null)
            return Denied(ApplicationErrorCodes.Channel.AccessDenied, "You do not have access to this channel");

        return new AuthorizationResult<Context>.Authorized(new Context(
            _channelId, ctx.Channel.Name, ctx.Channel.GuildId,
            ctx.GuildName ?? string.Empty,
            ctx.CallerUsername ?? string.Empty,
            ctx.CallerDisplayName ?? string.Empty));
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

    private static AuthorizationResult<Context> Denied(string code, string detail)
        => new AuthorizationResult<Context>.Denied(new ApplicationError(code, detail));
}
