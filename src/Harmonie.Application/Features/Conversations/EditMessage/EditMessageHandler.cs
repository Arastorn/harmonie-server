using Harmonie.Application.Common;
using Harmonie.Application.Interfaces;
using Harmonie.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Conversations.EditMessage;

public sealed class EditMessageHandler
{
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(5);

    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _conversationMessageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConversationMessageNotifier _conversationMessageNotifier;
    private readonly ILogger<EditMessageHandler> _logger;

    public EditMessageHandler(
        IConversationRepository conversationRepository,
        IMessageRepository conversationMessageRepository,
        IUnitOfWork unitOfWork,
        IConversationMessageNotifier conversationMessageNotifier,
        ILogger<EditMessageHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _conversationMessageRepository = conversationMessageRepository;
        _unitOfWork = unitOfWork;
        _conversationMessageNotifier = conversationMessageNotifier;
        _logger = logger;
    }

    public async Task<ApplicationResponse<EditMessageResponse>> HandleAsync(
        ConversationId conversationId,
        MessageId messageId,
        EditMessageRequest request,
        UserId callerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "EditConversationMessage started. ConversationId={ConversationId}, MessageId={MessageId}, CallerId={CallerId}",
            conversationId,
            messageId,
            callerId);

        var contentResult = MessageContent.Create(request.Content);
        if (contentResult.IsFailure || contentResult.Value is null)
        {
            _logger.LogWarning(
                "EditConversationMessage validation failed. ConversationId={ConversationId}, MessageId={MessageId}, CallerId={CallerId}, Error={Error}",
                conversationId,
                messageId,
                callerId,
                contentResult.Error);

            var code = MessageContentErrorCodeResolver.Resolve(request.Content);
            return ApplicationResponse<EditMessageResponse>.Fail(
                code,
                contentResult.Error ?? "Message content is invalid");
        }

        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            _logger.LogWarning(
                "EditConversationMessage failed because conversation was not found. ConversationId={ConversationId}",
                conversationId);

            return ApplicationResponse<EditMessageResponse>.Fail(
                ApplicationErrorCodes.Conversation.NotFound,
                "Conversation was not found");
        }

        if (conversation.User1Id != callerId && conversation.User2Id != callerId)
        {
            _logger.LogWarning(
                "EditConversationMessage access denied because caller is not a participant. ConversationId={ConversationId}, CallerId={CallerId}",
                conversationId,
                callerId);

            return ApplicationResponse<EditMessageResponse>.Fail(
                ApplicationErrorCodes.Conversation.AccessDenied,
                "You do not have access to this conversation");
        }

        var message = await _conversationMessageRepository.GetByIdAsync(messageId, cancellationToken);
        var messageConversationId = message?.ConversationId;
        if (message is null || messageConversationId is null || messageConversationId != conversationId)
        {
            _logger.LogWarning(
                "EditConversationMessage failed because message was not found. ConversationId={ConversationId}, MessageId={MessageId}",
                conversationId,
                messageId);

            return ApplicationResponse<EditMessageResponse>.Fail(
                ApplicationErrorCodes.Message.NotFound,
                "Message was not found");
        }

        if (message.AuthorUserId != callerId)
        {
            _logger.LogWarning(
                "EditConversationMessage forbidden because caller is not the author. ConversationId={ConversationId}, MessageId={MessageId}, CallerId={CallerId}",
                conversationId,
                messageId,
                callerId);

            return ApplicationResponse<EditMessageResponse>.Fail(
                ApplicationErrorCodes.Message.EditForbidden,
                "You can only edit your own messages");
        }

        var updateResult = message.UpdateContent(contentResult.Value);
        if (updateResult.IsFailure)
        {
            return ApplicationResponse<EditMessageResponse>.Fail(
                ApplicationErrorCodes.Common.DomainRuleViolation,
                updateResult.Error ?? "Message content update failed");
        }

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken);
        await _conversationMessageRepository.UpdateAsync(message, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var updatedAtUtc = message.UpdatedAtUtc;
        if (updatedAtUtc is null)
        {
            return ApplicationResponse<EditMessageResponse>.Fail(
                ApplicationErrorCodes.Common.InvalidState,
                "Message edit succeeded but updated timestamp is missing");
        }

        _logger.LogInformation(
            "EditConversationMessage succeeded. ConversationId={ConversationId}, MessageId={MessageId}, CallerId={CallerId}",
            conversationId,
            messageId,
            callerId);

        await NotifyMessageUpdatedSafelyAsync(
            new ConversationMessageUpdatedNotification(
                message.Id,
                messageConversationId,
                message.Content.Value,
                updatedAtUtc.Value));

        return ApplicationResponse<EditMessageResponse>.Ok(new EditMessageResponse(
            MessageId: message.Id.ToString(),
            ConversationId: messageConversationId.ToString(),
            AuthorUserId: message.AuthorUserId.ToString(),
            Content: message.Content.Value,
            CreatedAtUtc: message.CreatedAtUtc,
            UpdatedAtUtc: updatedAtUtc));
    }

    private async Task NotifyMessageUpdatedSafelyAsync(
        ConversationMessageUpdatedNotification notification)
    {
        await BestEffortNotificationHelper.TryNotifyAsync(
            token => _conversationMessageNotifier.NotifyMessageUpdatedAsync(notification, token),
            NotificationTimeout,
            _logger,
            "EditConversationMessage notification failed (best-effort). MessageId={MessageId}, ConversationId={ConversationId}",
            notification.MessageId,
            notification.ConversationId);
    }
}
