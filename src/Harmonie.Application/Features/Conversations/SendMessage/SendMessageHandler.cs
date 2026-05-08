using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Common;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Application.Services;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Conversations.SendMessage;

public sealed record SendConversationMessageInput(ConversationId ConversationId, string? Content, IReadOnlyList<Guid>? AttachmentFileIds = null, Guid? ReplyToMessageId = null);

public sealed class SendMessageHandler : IAuthenticatedHandler<SendConversationMessageInput, SendMessageResponse>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationParticipantRepository _participantRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageAttachmentRepository _messageAttachmentRepository;
    private readonly MessageAttachmentResolver _messageAttachmentResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConversationMessageNotifier _conversationMessageNotifier;
    private readonly LinkPreviewResolutionService _linkPreviewService;
    private readonly ILogger<SendMessageHandler> _logger;

    public SendMessageHandler(
        IConversationRepository conversationRepository,
        IConversationParticipantRepository participantRepository,
        IMessageRepository messageRepository,
        IMessageAttachmentRepository messageAttachmentRepository,
        MessageAttachmentResolver messageAttachmentResolver,
        IUnitOfWork unitOfWork,
        IConversationMessageNotifier conversationMessageNotifier,
        LinkPreviewResolutionService linkPreviewService,
        ILogger<SendMessageHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _messageRepository = messageRepository;
        _messageAttachmentRepository = messageAttachmentRepository;
        _messageAttachmentResolver = messageAttachmentResolver;
        _unitOfWork = unitOfWork;
        _conversationMessageNotifier = conversationMessageNotifier;
        _linkPreviewService = linkPreviewService;
        _logger = logger;
    }

    public async Task<ApplicationResponse<SendMessageResponse>> HandleAsync(
        SendConversationMessageInput request,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = new ConversationSendMessageScope(
            request.ConversationId,
            _conversationRepository,
            _participantRepository,
            _conversationMessageNotifier,
            _linkPreviewService,
            _logger);

        var result = await MessageSendOrchestrator.SendAsync(
            scope,
            new MessageScope.Conversation(request.ConversationId),
            request.Content,
            request.AttachmentFileIds,
            request.ReplyToMessageId,
            currentUserId,
            _messageRepository,
            _messageAttachmentRepository,
            _messageAttachmentResolver,
            _unitOfWork,
            cancellationToken);

        if (!result.Success)
            return ApplicationResponse<SendMessageResponse>.Fail(result.Error!);

        return ApplicationResponse<SendMessageResponse>.Ok(new SendMessageResponse(
            MessageId: result.Data!.MessageId,
            ConversationId: request.ConversationId.Value,
            AuthorUserId: result.Data.AuthorUserId,
            Content: result.Data.Content,
            Attachments: result.Data.Attachments,
            ReplyTo: result.Data.ReplyTo,
            CreatedAtUtc: result.Data.CreatedAtUtc));
    }
}
