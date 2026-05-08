using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Services;
using Harmonie.Domain.Entities.Conversations;
using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Conversations.SendMessage;

/// <summary>
/// Conversation-specific implementation of <see cref="ISendMessageScope"/>.
/// </summary>
public sealed class ConversationSendMessageScope : ISendMessageScope
{
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(5);

    private sealed record ConversationSendContext(
        ConversationId ConversationId,
        string? ConversationName,
        string ConversationType,
        IReadOnlyList<ConversationParticipant> AllParticipants,
        ConversationType DomainType,
        string CallerUsername,
        string CallerDisplayName) : SendScopeContext;

    private readonly ConversationId _conversationId;
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationParticipantRepository _participantRepository;
    private readonly IConversationMessageNotifier _conversationMessageNotifier;
    private readonly LinkPreviewResolutionService _linkPreviewService;
    private readonly ILogger<ConversationSendMessageScope> _logger;

    public ConversationSendMessageScope(
        ConversationId conversationId,
        IConversationRepository conversationRepository,
        IConversationParticipantRepository participantRepository,
        IConversationMessageNotifier conversationMessageNotifier,
        LinkPreviewResolutionService linkPreviewService,
        ILogger<ConversationSendMessageScope> logger)
    {
        _conversationId = conversationId;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _conversationMessageNotifier = conversationMessageNotifier;
        _linkPreviewService = linkPreviewService;
        _logger = logger;
    }

    public async Task<AuthorizationResult> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var access = await _conversationRepository.GetByIdWithAllParticipantsAsync(_conversationId, caller, ct);
        if (access is null)
        {
            return new AuthorizationResult(null, new ApplicationError(
                ApplicationErrorCodes.Conversation.NotFound,
                "Conversation was not found"));
        }
        if (access.CallerParticipant is null)
        {
            return new AuthorizationResult(null, new ApplicationError(
                ApplicationErrorCodes.Conversation.AccessDenied,
                "You do not have access to this conversation"));
        }

        var context = new ConversationSendContext(
            _conversationId,
            access.Conversation.Name,
            access.Conversation.Type.ToString(),
            access.AllParticipants,
            access.Conversation.Type,
            access.CallerUsername ?? string.Empty,
            access.CallerDisplayName ?? string.Empty);

        return new AuthorizationResult(context, null);
    }

    public async Task ApplyInTransactionSideEffectsAsync(SendScopeContext context, CancellationToken ct)
    {
        var ctx = (ConversationSendContext)context;

        if (ctx.DomainType != ConversationType.Direct)
            return;

        var hidden = ctx.AllParticipants
            .Where(p => p.HiddenAtUtc is not null)
            .ToArray();

        if (hidden.Length == 0)
            return;

        foreach (var p in hidden)
            p.Unhide();

        await _participantRepository.UpdateRangeAsync(hidden, ct);
    }

    public async Task NotifyMessageCreatedAsync(
        SendScopeContext context,
        Message message,
        IReadOnlyList<MessageAttachmentDto> attachments,
        ReplyPreviewDto? replyTo,
        CancellationToken ct)
    {
        var ctx = (ConversationSendContext)context;

        var notification = new ConversationMessageCreatedNotification(
            message.Id,
            ctx.ConversationId,
            ctx.ConversationName,
            ctx.ConversationType,
            message.AuthorUserId,
            ctx.CallerUsername,
            ctx.CallerDisplayName,
            message.Content?.Value,
            attachments,
            replyTo,
            message.CreatedAtUtc);

        await BestEffortNotificationHelper.TryNotifyAsync(
            token => _conversationMessageNotifier.NotifyMessageCreatedAsync(notification, token),
            NotificationTimeout,
            _logger,
            "SendConversationMessage notification failed (best-effort). MessageId={MessageId}, ConversationId={ConversationId}",
            notification.MessageId,
            notification.ConversationId);
    }

    public void ScheduleLinkPreviewResolution(
        SendScopeContext context,
        Message message,
        IReadOnlyList<Uri> urls,
        CancellationToken ct)
    {
        var ctx = (ConversationSendContext)context;

        // TODO: Replace fire-and-forget with a domain event + dedicated background worker
        _ = _linkPreviewService.ResolveAndNotifyForConversationAsync(
            message.Id,
            ctx.ConversationId,
            ctx.ConversationName,
            ctx.ConversationType,
            urls,
            ct);
    }
}
