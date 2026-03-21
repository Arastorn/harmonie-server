using Harmonie.Application.Common;
using Harmonie.Application.Common.Uploads;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Common;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Uploads;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Channels.DeleteMessageAttachment;

public sealed class DeleteMessageAttachmentHandler
{
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly UploadedFileCleanupService _uploadedFileCleanupService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteMessageAttachmentHandler> _logger;

    public DeleteMessageAttachmentHandler(
        IGuildChannelRepository guildChannelRepository,
        IMessageRepository messageRepository,
        UploadedFileCleanupService uploadedFileCleanupService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteMessageAttachmentHandler> logger)
    {
        _guildChannelRepository = guildChannelRepository;
        _messageRepository = messageRepository;
        _uploadedFileCleanupService = uploadedFileCleanupService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApplicationResponse<bool>> HandleAsync(
        GuildChannelId channelId,
        MessageId messageId,
        UploadedFileId attachmentId,
        UserId callerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DeleteMessageAttachment started. ChannelId={ChannelId}, MessageId={MessageId}, AttachmentId={AttachmentId}, CallerId={CallerId}",
            channelId,
            messageId,
            attachmentId,
            callerId);

        var ctx = await _guildChannelRepository.GetWithCallerRoleAsync(channelId, callerId, cancellationToken);
        if (ctx is null)
        {
            _logger.LogWarning(
                "DeleteMessageAttachment failed because channel was not found. ChannelId={ChannelId}",
                channelId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Channel.NotFound,
                "Channel was not found");
        }

        if (ctx.Channel.Type != GuildChannelType.Text)
        {
            _logger.LogWarning(
                "DeleteMessageAttachment failed because channel is not text. ChannelId={ChannelId}, ChannelType={ChannelType}",
                channelId,
                ctx.Channel.Type);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Channel.NotText,
                "Attachments can only be deleted from messages in text channels");
        }

        if (ctx.CallerRole is null)
        {
            _logger.LogWarning(
                "DeleteMessageAttachment access denied because caller is not a member. ChannelId={ChannelId}, GuildId={GuildId}, CallerId={CallerId}",
                channelId,
                ctx.Channel.GuildId,
                callerId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Channel.AccessDenied,
                "You do not have access to this channel");
        }

        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken);
        var messageChannelId = message?.ChannelId;
        if (message is null || messageChannelId is null || messageChannelId != channelId)
        {
            _logger.LogWarning(
                "DeleteMessageAttachment failed because message was not found. ChannelId={ChannelId}, MessageId={MessageId}",
                channelId,
                messageId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Message.NotFound,
                "Message was not found");
        }

        if (message.AuthorUserId != callerId)
        {
            _logger.LogWarning(
                "DeleteMessageAttachment forbidden because caller is not the author. ChannelId={ChannelId}, MessageId={MessageId}, CallerId={CallerId}",
                channelId,
                messageId,
                callerId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Message.DeleteForbidden,
                "You can only delete attachments from your own messages");
        }

        var removeAttachmentResult = message.RemoveAttachment(attachmentId);
        if (removeAttachmentResult.IsFailure)
        {
            _logger.LogWarning(
                "DeleteMessageAttachment failed because attachment was not found on message. ChannelId={ChannelId}, MessageId={MessageId}, AttachmentId={AttachmentId}",
                channelId,
                messageId,
                attachmentId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Message.AttachmentNotFound,
                removeAttachmentResult.Error ?? "Attachment was not found on message");
        }

        await using (var transaction = await _unitOfWork.BeginAsync(cancellationToken))
        {
            await _messageRepository.UpdateAsync(message, cancellationToken);
            await _messageRepository.RemoveAttachmentAsync(message.Id, attachmentId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await _uploadedFileCleanupService.DeleteIfExistsAsync(attachmentId, cancellationToken);

        _logger.LogInformation(
            "DeleteMessageAttachment succeeded. ChannelId={ChannelId}, MessageId={MessageId}, AttachmentId={AttachmentId}, CallerId={CallerId}",
            channelId,
            messageId,
            attachmentId,
            callerId);

        return ApplicationResponse<bool>.Ok(true);
    }
}
