using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Services;
using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Channels.SendMessage;

/// <summary>
/// Channel-specific implementation of <see cref="ISendMessageScope{TContext}"/>.
/// </summary>
public sealed class ChannelSendMessageScope : ISendMessageScope<ChannelSendMessageScope.Context>
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
    private readonly ITextChannelNotifier _textChannelNotifier;
    private readonly LinkPreviewResolutionService _linkPreviewService;
    private readonly ILogger<ChannelSendMessageScope> _logger;

    public ChannelSendMessageScope(
        GuildChannelId channelId,
        IGuildChannelRepository guildChannelRepository,
        ITextChannelNotifier textChannelNotifier,
        LinkPreviewResolutionService linkPreviewService,
        ILogger<ChannelSendMessageScope> logger)
    {
        _channelId = channelId;
        _guildChannelRepository = guildChannelRepository;
        _textChannelNotifier = textChannelNotifier;
        _linkPreviewService = linkPreviewService;
        _logger = logger;
    }

    public async Task<AuthorizationResult<Context>> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var ctx = await _guildChannelRepository.GetWithCallerRoleAsync(_channelId, caller, ct);
        if (ctx is null)
        {
            return new AuthorizationResult<Context>.Denied(new ApplicationError(
                ApplicationErrorCodes.Channel.NotFound,
                "Channel was not found"));
        }

        if (ctx.Channel.Type != GuildChannelType.Text)
        {
            return new AuthorizationResult<Context>.Denied(new ApplicationError(
                ApplicationErrorCodes.Channel.NotText,
                "Messages can only be sent to text channels"));
        }

        if (ctx.CallerRole is null)
        {
            return new AuthorizationResult<Context>.Denied(new ApplicationError(
                ApplicationErrorCodes.Channel.AccessDenied,
                "You do not have access to this channel"));
        }

        return new AuthorizationResult<Context>.Authorized(new Context(
            _channelId,
            ctx.Channel.Name,
            ctx.Channel.GuildId,
            ctx.GuildName ?? string.Empty,
            ctx.CallerUsername ?? string.Empty,
            ctx.CallerDisplayName ?? string.Empty));
    }

    public Task ApplyInTransactionSideEffectsAsync(Context context, CancellationToken ct)
        => Task.CompletedTask;

    public async Task NotifyMessageCreatedAsync(
        Context context,
        Message message,
        IReadOnlyList<MessageAttachmentDto> attachments,
        ReplyPreviewDto? replyTo,
        CancellationToken ct)
    {
        var notification = new TextChannelMessageCreatedNotification(
            message.Id,
            context.ChannelId,
            context.ChannelName,
            context.GuildId,
            context.GuildName,
            message.AuthorUserId,
            context.CallerUsername,
            context.CallerDisplayName,
            message.Content?.Value,
            attachments,
            replyTo,
            message.CreatedAtUtc);

        await BestEffortNotificationHelper.TryNotifyAsync(
            token => _textChannelNotifier.NotifyMessageCreatedAsync(notification, token),
            NotificationTimeout,
            _logger,
            "SendMessage notification failed (best-effort). MessageId={MessageId}, ChannelId={ChannelId}",
            notification.MessageId,
            notification.ChannelId);
    }

    public void ScheduleLinkPreviewResolution(
        Context context,
        Message message,
        IReadOnlyList<Uri> urls,
        CancellationToken ct)
    {
        // TODO: Replace fire-and-forget with a domain event + dedicated background worker
        _ = _linkPreviewService.ResolveAndNotifyForChannelAsync(
            message.Id,
            context.ChannelId,
            context.ChannelName,
            context.GuildId,
            context.GuildName,
            urls,
            ct);
    }
}
