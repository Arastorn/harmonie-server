using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Common;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Conversations.DeleteMessage;

public sealed class DeleteMessageHandler
{
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(5);

    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _conversationMessageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConversationMessageNotifier _conversationMessageNotifier;
    private readonly ILogger<DeleteMessageHandler> _logger;

    public DeleteMessageHandler(
        IConversationRepository conversationRepository,
        IMessageRepository conversationMessageRepository,
        IUnitOfWork unitOfWork,
        IConversationMessageNotifier conversationMessageNotifier,
        ILogger<DeleteMessageHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _conversationMessageRepository = conversationMessageRepository;
        _unitOfWork = unitOfWork;
        _conversationMessageNotifier = conversationMessageNotifier;
        _logger = logger;
    }

    public async Task<ApplicationResponse<bool>> HandleAsync(
        ConversationId conversationId,
        MessageId messageId,
        UserId callerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DeleteConversationMessage started. ConversationId={ConversationId}, MessageId={MessageId}, CallerId={CallerId}",
            conversationId,
            messageId,
            callerId);

        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            _logger.LogWarning(
                "DeleteConversationMessage failed because conversation was not found. ConversationId={ConversationId}",
                conversationId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Conversation.NotFound,
                "Conversation was not found");
        }

        if (conversation.User1Id != callerId && conversation.User2Id != callerId)
        {
            _logger.LogWarning(
                "DeleteConversationMessage access denied because caller is not a participant. ConversationId={ConversationId}, CallerId={CallerId}",
                conversationId,
                callerId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Conversation.AccessDenied,
                "You do not have access to this conversation");
        }

        var message = await _conversationMessageRepository.GetByIdAsync(messageId, cancellationToken);
        var messageConversationId = message?.ConversationId;
        if (message is null || messageConversationId is null || messageConversationId != conversationId)
        {
            _logger.LogWarning(
                "DeleteConversationMessage failed because message was not found. ConversationId={ConversationId}, MessageId={MessageId}",
                conversationId,
                messageId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Message.NotFound,
                "Message was not found");
        }

        if (message.AuthorUserId != callerId)
        {
            _logger.LogWarning(
                "DeleteConversationMessage forbidden because caller is not the author. ConversationId={ConversationId}, MessageId={MessageId}, CallerId={CallerId}",
                conversationId,
                messageId,
                callerId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Message.DeleteForbidden,
                "You can only delete your own messages");
        }

        var deleteResult = message.Delete();
        if (deleteResult.IsFailure)
        {
            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Common.DomainRuleViolation,
                deleteResult.Error ?? "Message deletion failed");
        }

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken);
        await _conversationMessageRepository.SoftDeleteAsync(message, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "DeleteConversationMessage succeeded. ConversationId={ConversationId}, MessageId={MessageId}, CallerId={CallerId}",
            conversationId,
            messageId,
            callerId);

        await NotifyMessageDeletedSafelyAsync(
            new ConversationMessageDeletedNotification(messageId, conversationId));

        return ApplicationResponse<bool>.Ok(true);
    }

    private async Task NotifyMessageDeletedSafelyAsync(
        ConversationMessageDeletedNotification notification)
    {
        await BestEffortNotificationHelper.TryNotifyAsync(
            token => _conversationMessageNotifier.NotifyMessageDeletedAsync(notification, token),
            NotificationTimeout,
            _logger,
            "DeleteConversationMessage notification failed (best-effort). MessageId={MessageId}, ConversationId={ConversationId}",
            notification.MessageId,
            notification.ConversationId);
    }
}
