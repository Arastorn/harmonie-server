using Harmonie.Application.Common;
using Harmonie.Application.Interfaces;
using Harmonie.Domain.Entities;
using Harmonie.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Conversations.SendMessage;

public sealed class SendMessageHandler
{
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(5);

    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _conversationMessageRepository;
    private readonly MessageAttachmentResolver _messageAttachmentResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConversationMessageNotifier _conversationMessageNotifier;
    private readonly ILogger<SendMessageHandler> _logger;

    public SendMessageHandler(
        IConversationRepository conversationRepository,
        IMessageRepository conversationMessageRepository,
        MessageAttachmentResolver messageAttachmentResolver,
        IUnitOfWork unitOfWork,
        IConversationMessageNotifier conversationMessageNotifier,
        ILogger<SendMessageHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _conversationMessageRepository = conversationMessageRepository;
        _messageAttachmentResolver = messageAttachmentResolver;
        _unitOfWork = unitOfWork;
        _conversationMessageNotifier = conversationMessageNotifier;
        _logger = logger;
    }

    public async Task<ApplicationResponse<SendMessageResponse>> HandleAsync(
        ConversationId conversationId,
        SendMessageRequest request,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "SendConversationMessage started. ConversationId={ConversationId}, UserId={UserId}",
            conversationId,
            currentUserId);

        var contentResult = MessageContent.Create(request.Content);
        if (contentResult.IsFailure || contentResult.Value is null)
        {
            _logger.LogWarning(
                "SendConversationMessage validation failed. ConversationId={ConversationId}, UserId={UserId}, Error={Error}",
                conversationId,
                currentUserId,
                contentResult.Error);

            var code = MessageContentErrorCodeResolver.Resolve(request.Content);
            return ApplicationResponse<SendMessageResponse>.Fail(
                code,
                contentResult.Error ?? "Message content is invalid");
        }

        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            _logger.LogWarning(
                "SendConversationMessage failed because conversation was not found. ConversationId={ConversationId}, UserId={UserId}",
                conversationId,
                currentUserId);

            return ApplicationResponse<SendMessageResponse>.Fail(
                ApplicationErrorCodes.Conversation.NotFound,
                "Conversation was not found");
        }

        if (conversation.User1Id != currentUserId && conversation.User2Id != currentUserId)
        {
            _logger.LogWarning(
                "SendConversationMessage access denied. ConversationId={ConversationId}, UserId={UserId}",
                conversationId,
                currentUserId);

            return ApplicationResponse<SendMessageResponse>.Fail(
                ApplicationErrorCodes.Conversation.AccessDenied,
                "You do not have access to this conversation");
        }

        var attachmentResolution = await _messageAttachmentResolver.ResolveAsync(
            request.AttachmentFileIds,
            currentUserId,
            cancellationToken);
        if (!attachmentResolution.Success)
        {
            return ApplicationResponse<SendMessageResponse>.Fail(
                ApplicationErrorCodes.Common.ValidationFailed,
                "Request validation failed",
                EndpointExtensions.SingleValidationError(
                    nameof(request.AttachmentFileIds),
                    ApplicationErrorCodes.Validation.Invalid,
                    attachmentResolution.Error ?? "Attachments are invalid"));
        }

        var messageResult = Message.CreateForConversation(
            conversationId,
            currentUserId,
            contentResult.Value,
            attachmentResolution.Attachments);
        if (messageResult.IsFailure || messageResult.Value is null)
        {
            _logger.LogWarning(
                "SendConversationMessage domain creation failed. ConversationId={ConversationId}, UserId={UserId}, Error={Error}",
                conversationId,
                currentUserId,
                messageResult.Error);

            return ApplicationResponse<SendMessageResponse>.Fail(
                ApplicationErrorCodes.Common.DomainRuleViolation,
                messageResult.Error ?? "Unable to create conversation message");
        }

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken);
        await _conversationMessageRepository.AddAsync(messageResult.Value, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var messageConversationId = messageResult.Value.ConversationId;
        if (messageConversationId is null)
        {
            return ApplicationResponse<SendMessageResponse>.Fail(
                ApplicationErrorCodes.Common.InvalidState,
                "Conversation message creation succeeded but conversation ID is missing");
        }

        await NotifyMessageCreatedSafelyAsync(
            new ConversationMessageCreatedNotification(
                messageResult.Value.Id,
                messageConversationId,
                messageResult.Value.AuthorUserId,
                messageResult.Value.Content.Value,
                messageResult.Value.Attachments.Select(MessageAttachmentDto.FromDomain).ToArray(),
                messageResult.Value.CreatedAtUtc));

        _logger.LogInformation(
            "SendConversationMessage succeeded. MessageId={MessageId}, ConversationId={ConversationId}, UserId={UserId}",
            messageResult.Value.Id,
            messageConversationId,
            messageResult.Value.AuthorUserId);

        return ApplicationResponse<SendMessageResponse>.Ok(new SendMessageResponse(
            MessageId: messageResult.Value.Id.ToString(),
            ConversationId: messageConversationId.ToString(),
            AuthorUserId: messageResult.Value.AuthorUserId.ToString(),
            Content: messageResult.Value.Content.Value,
            Attachments: messageResult.Value.Attachments.Select(MessageAttachmentDto.FromDomain).ToArray(),
            CreatedAtUtc: messageResult.Value.CreatedAtUtc));
    }

    private async Task NotifyMessageCreatedSafelyAsync(
        ConversationMessageCreatedNotification notification)
    {
        await BestEffortNotificationHelper.TryNotifyAsync(
            token => _conversationMessageNotifier.NotifyMessageCreatedAsync(notification, token),
            NotificationTimeout,
            _logger,
            "SendConversationMessage notification failed (best-effort). MessageId={MessageId}, ConversationId={ConversationId}",
            notification.MessageId,
            notification.ConversationId);
    }
}
