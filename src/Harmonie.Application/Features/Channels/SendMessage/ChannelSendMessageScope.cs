using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Services;
using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Channels.SendMessage;

/// <summary>
/// Channel-specific implementation of <see cref="ISendMessageScope"/>.
/// </summary>
internal sealed class ChannelSendMessageScope : ISendMessageScope
{
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(5);

    private readonly GuildChannelId _channelId;
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly ITextChannelNotifier _textChannelNotifier;
    private readonly LinkPreviewResolutionService _linkPreviewService;
    private readonly ILogger _logger;

    private ChannelAccessContext? _ctx;

    public ChannelSendMessageScope(
        GuildChannelId channelId,
        IGuildChannelRepository guildChannelRepository,
        ITextChannelNotifier textChannelNotifier,
        LinkPreviewResolutionService linkPreviewService,
        ILogger logger)
    {
        _channelId = channelId;
        _guildChannelRepository = guildChannelRepository;
        _textChannelNotifier = textChannelNotifier;
        _linkPreviewService = linkPreviewService;
        _logger = logger;
    }

    public async Task<ApplicationError?> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var ctx = await _guildChannelRepository.GetWithCallerRoleAsync(_channelId, caller, ct);
        if (ctx is null)
        {
            return new ApplicationError(
                ApplicationErrorCodes.Channel.NotFound,
                "Channel was not found");
        }

        if (ctx.Channel.Type != GuildChannelType.Text)
        {
            return new ApplicationError(
                ApplicationErrorCodes.Channel.NotText,
                "Messages can only be sent to text channels");
        }

        if (ctx.CallerRole is null)
        {
            return new ApplicationError(
                ApplicationErrorCodes.Channel.AccessDenied,
                "You do not have access to this channel");
        }

        _ctx = ctx;
        return null; // authorized
    }

    public Task PreparePostCommitAsync(CancellationToken ct) => Task.CompletedTask;

    public async Task NotifyMessageCreatedAsync(
        Message message,
        IReadOnlyList<MessageAttachment> attachments,
        ReplyPreviewDto? replyTo,
        CancellationToken ct)
    {
        if (_ctx is null)
            return;

        var notification = new TextChannelMessageCreatedNotification(
            message.Id,
            _channelId,
            _ctx.Channel.Name,
            _ctx.Channel.GuildId,
            _ctx.GuildName ?? string.Empty,
            message.AuthorUserId,
            _ctx.CallerUsername ?? string.Empty,
            _ctx.CallerDisplayName,
            message.Content?.Value,
            attachments.Select(MessageAttachmentDto.FromDomain).ToArray(),
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

    public void ResolveLinkPreviewsAsync(Message message, IReadOnlyList<Uri> urls, CancellationToken ct)
    {
        if (_ctx is null)
            return;

        // TODO: Replace fire-and-forget with a domain event + dedicated background worker
        _ = _linkPreviewService.ResolveAndNotifyForChannelAsync(
            message.Id,
            _channelId,
            _ctx.Channel.Name,
            _ctx.Channel.GuildId,
            _ctx.GuildName ?? string.Empty,
            urls,
            ct);
    }
}
