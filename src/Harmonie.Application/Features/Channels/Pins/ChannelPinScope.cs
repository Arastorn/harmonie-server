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

namespace Harmonie.Application.Features.Channels.Pins;

public sealed class ChannelPinScope : IPinScope<ChannelPinScope.Context>
{
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(5);

    public sealed record Context(
        GuildChannelId ChannelId,
        string ChannelName,
        GuildId GuildId,
        string GuildName,
        string CallerUsername,
        string? CallerDisplayName) : ScopeContext;

    private readonly GuildChannelId _channelId;
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IPinNotifier _pinNotifier;
    private readonly ILogger<ChannelPinScope> _logger;

    public ChannelPinScope(
        GuildChannelId channelId,
        IGuildChannelRepository guildChannelRepository,
        IPinNotifier pinNotifier,
        ILogger<ChannelPinScope> logger)
    {
        _channelId = channelId;
        _guildChannelRepository = guildChannelRepository;
        _pinNotifier = pinNotifier;
        _logger = logger;
    }

    public async Task<AuthorizationResult<Context>> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var ctx = await _guildChannelRepository.GetWithCallerRoleAsync(_channelId, caller, ct);
        if (ctx is null)
            return Denied(ApplicationErrorCodes.Channel.NotFound, "Channel was not found");

        if (ctx.Channel.Type != GuildChannelType.Text)
            return Denied(ApplicationErrorCodes.Channel.NotText, "Messages can only be pinned in text channels");

        if (ctx.CallerRole is null)
            return Denied(ApplicationErrorCodes.Channel.AccessDenied, "You do not have access to this channel");

        return new AuthorizationResult<Context>.Authorized(new Context(
            _channelId, ctx.Channel.Name, ctx.Channel.GuildId,
            ctx.GuildName ?? string.Empty,
            ctx.CallerUsername ?? string.Empty,
            ctx.CallerDisplayName));
    }

    public async Task NotifyPinAddedAsync(
        Context context, MessageId messageId, UserId userId, DateTime pinnedAtUtc, CancellationToken ct)
    {
        var notification = new ChannelPinAddedNotification(
            messageId, context.ChannelId, context.ChannelName,
            context.GuildId, context.GuildName,
            userId, context.CallerUsername, context.CallerDisplayName, pinnedAtUtc);

        await BestEffortNotificationHelper.TryNotifyAsync(
            token => _pinNotifier.NotifyMessagePinnedInChannelAsync(notification, token),
            NotificationTimeout, _logger,
            "Channel pin notification failed (best-effort). MessageId={MessageId}, ChannelId={ChannelId}",
            messageId, context.ChannelId);
    }

    public async Task NotifyPinRemovedAsync(
        Context context, MessageId messageId, UserId userId, DateTime unpinnedAtUtc, CancellationToken ct)
    {
        var notification = new ChannelPinRemovedNotification(
            messageId, context.ChannelId, context.ChannelName,
            context.GuildId, context.GuildName,
            userId, context.CallerUsername, context.CallerDisplayName, unpinnedAtUtc);

        await BestEffortNotificationHelper.TryNotifyAsync(
            token => _pinNotifier.NotifyMessageUnpinnedInChannelAsync(notification, token),
            NotificationTimeout, _logger,
            "Channel unpin notification failed (best-effort). MessageId={MessageId}, ChannelId={ChannelId}",
            messageId, context.ChannelId);
    }

    private static AuthorizationResult<Context> Denied(string code, string detail)
        => new AuthorizationResult<Context>.Denied(new ApplicationError(code, detail));
}
