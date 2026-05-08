using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Services;
using Harmonie.Domain.Entities.Conversations;
using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Conversations.SendMessage;

/// <summary>
/// Conversation-specific implementation of <see cref="ISendMessageScope"/>.
/// </summary>
internal sealed class ConversationSendMessageScope : ISendMessageScope
{
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(5);

    private readonly ConversationId _conversationId;
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationParticipantRepository _participantRepository;
    private readonly IConversationMessageNotifier _conversationMessageNotifier;
    private readonly LinkPreviewResolutionService _linkPreviewService;
    private readonly ILogger _logger;

    private ConversationAccessWithAllParticipants? _access;
    private IReadOnlyList<ConversationParticipant>? _hiddenParticipants;

    public ConversationSendMessageScope(
        ConversationId conversationId,
        IConversationRepository conversationRepository,
        IConversationParticipantRepository participantRepository,
        IConversationMessageNotifier conversationMessageNotifier,
        LinkPreviewResolutionService linkPreviewService,
        ILogger logger)
    {
        _conversationId = conversationId;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _conversationMessageNotifier = conversationMessageNotifier;
        _linkPreviewService = linkPreviewService;
        _logger = logger;
    }

    public async Task<ApplicationError?> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var access = await _conversationRepository.GetByIdWithAllParticipantsAsync(_conversationId, caller, ct);
        if (access is null)
        {
            return new ApplicationError(
                ApplicationErrorCodes.Conversation.NotFound,
                "Conversation was not found");
        }
        if (access.CallerParticipant is null)
        {
            return new ApplicationError(
                ApplicationErrorCodes.Conversation.AccessDenied,
                "You do not have access to this conversation");
        }

        _access = access;
        return null; // authorized
    }

    public Task PreparePostCommitAsync(CancellationToken ct)
    {
        _hiddenParticipants = Array.Empty<ConversationParticipant>();

        if (_access?.Conversation.Type == ConversationType.Direct)
        {
            var hidden = _access.AllParticipants
                .Where(p => p.HiddenAtUtc is not null)
                .ToArray();
            foreach (var p in hidden)
                p.Unhide();

            if (hidden.Length > 0)
            {
                _hiddenParticipants = hidden;
                return _participantRepository.UpdateRangeAsync(hidden, ct);
            }
        }

        return Task.CompletedTask;
    }

    public async Task NotifyMessageCreatedAsync(
        Message message,
        IReadOnlyList<MessageAttachment> attachments,
        ReplyPreviewDto? replyTo,
        CancellationToken ct)
    {
        if (_access is null)
            return;

        var notification = new ConversationMessageCreatedNotification(
            message.Id,
            _conversationId,
            _access.Conversation.Name,
            _access.Conversation.Type.ToString(),
            message.AuthorUserId,
            _access.CallerUsername ?? string.Empty,
            _access.CallerDisplayName,
            message.Content?.Value,
            attachments.Select(MessageAttachmentDto.FromDomain).ToArray(),
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

    public void ResolveLinkPreviewsAsync(Message message, IReadOnlyList<Uri> urls, CancellationToken ct)
    {
        if (_access is null)
            return;

        // TODO: Replace fire-and-forget with a domain event + dedicated background worker
        _ = _linkPreviewService.ResolveAndNotifyForConversationAsync(
            message.Id,
            _conversationId,
            _access.Conversation.Name,
            _access.Conversation.Type.ToString(),
            urls,
            ct);
    }
}
