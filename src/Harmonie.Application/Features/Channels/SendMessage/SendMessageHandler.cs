using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Common;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Application.Services;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Channels.SendMessage;

public sealed record SendChannelMessageInput(GuildChannelId ChannelId, string? Content, IReadOnlyList<Guid>? AttachmentFileIds = null, Guid? ReplyToMessageId = null);

public sealed class SendMessageHandler : IAuthenticatedHandler<SendChannelMessageInput, SendMessageResponse>
{
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageAttachmentRepository _messageAttachmentRepository;
    private readonly MessageAttachmentResolver _messageAttachmentResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITextChannelNotifier _textChannelNotifier;
    private readonly LinkPreviewResolutionService _linkPreviewService;
    private readonly ILogger<SendMessageHandler> _logger;

    public SendMessageHandler(
        IGuildChannelRepository guildChannelRepository,
        IMessageRepository messageRepository,
        IMessageAttachmentRepository messageAttachmentRepository,
        MessageAttachmentResolver messageAttachmentResolver,
        IUnitOfWork unitOfWork,
        ITextChannelNotifier textChannelNotifier,
        LinkPreviewResolutionService linkPreviewService,
        ILogger<SendMessageHandler> logger)
    {
        _guildChannelRepository = guildChannelRepository;
        _messageRepository = messageRepository;
        _messageAttachmentRepository = messageAttachmentRepository;
        _messageAttachmentResolver = messageAttachmentResolver;
        _unitOfWork = unitOfWork;
        _textChannelNotifier = textChannelNotifier;
        _linkPreviewService = linkPreviewService;
        _logger = logger;
    }

    public async Task<ApplicationResponse<SendMessageResponse>> HandleAsync(
        SendChannelMessageInput request,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = new ChannelSendMessageScope(
            request.ChannelId,
            _guildChannelRepository,
            _textChannelNotifier,
            _linkPreviewService,
            _logger);

        var result = await MessageSendOrchestrator.SendAsync(
            scope,
            new MessageScope.Channel(request.ChannelId),
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
            ChannelId: request.ChannelId.Value,
            AuthorUserId: result.Data.AuthorUserId,
            Content: result.Data.Content,
            Attachments: result.Data.Attachments,
            ReplyTo: result.Data.ReplyTo,
            CreatedAtUtc: result.Data.CreatedAtUtc));
    }
}
