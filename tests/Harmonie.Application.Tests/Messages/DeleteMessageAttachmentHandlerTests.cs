using FluentAssertions;
using Harmonie.Application.Common;
using Harmonie.Application.Features.Channels.DeleteMessageAttachment;
using Harmonie.Application.Interfaces;
using Harmonie.Domain.Entities;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Harmonie.Application.Tests;

public sealed class DeleteMessageAttachmentHandlerTests
{
    private readonly Mock<IGuildChannelRepository> _guildChannelRepositoryMock;
    private readonly Mock<IMessageRepository> _messageRepositoryMock;
    private readonly Mock<IUploadedFileRepository> _uploadedFileRepositoryMock;
    private readonly Mock<IObjectStorageService> _objectStorageServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUnitOfWorkTransaction> _transactionMock;
    private readonly DeleteMessageAttachmentHandler _handler;

    public DeleteMessageAttachmentHandlerTests()
    {
        _guildChannelRepositoryMock = new Mock<IGuildChannelRepository>();
        _messageRepositoryMock = new Mock<IMessageRepository>();
        _uploadedFileRepositoryMock = new Mock<IUploadedFileRepository>();
        _objectStorageServiceMock = new Mock<IObjectStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _transactionMock = new Mock<IUnitOfWorkTransaction>();

        _unitOfWorkMock
            .Setup(x => x.BeginAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _transactionMock
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        _handler = new DeleteMessageAttachmentHandler(
            _guildChannelRepositoryMock.Object,
            _messageRepositoryMock.Object,
            new UploadedFileCleanupService(
                _uploadedFileRepositoryMock.Object,
                _objectStorageServiceMock.Object,
                NullLogger<UploadedFileCleanupService>.Instance),
            _unitOfWorkMock.Object,
            NullLogger<DeleteMessageAttachmentHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenChannelDoesNotExist_ShouldReturnChannelNotFound()
    {
        var channelId = GuildChannelId.New();
        var messageId = MessageId.New();
        var attachmentId = UploadedFileId.New();
        var callerId = UserId.New();

        _guildChannelRepositoryMock
            .Setup(x => x.GetWithCallerRoleAsync(channelId, callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChannelAccessContext?)null);

        var response = await _handler.HandleAsync(channelId, messageId, attachmentId, callerId);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.Channel.NotFound);
    }

    [Fact]
    public async Task HandleAsync_WhenCallerIsNotAuthor_ShouldReturnDeleteForbidden()
    {
        var channel = CreateChannel();
        var callerId = UserId.New();
        var authorId = UserId.New();
        var attachmentId = UploadedFileId.New();
        var message = CreateMessage(channel.Id, authorId, attachmentId);

        _guildChannelRepositoryMock
            .Setup(x => x.GetWithCallerRoleAsync(channel.Id, callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChannelAccessContext(channel, GuildRole.Member));

        _messageRepositoryMock
            .Setup(x => x.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var response = await _handler.HandleAsync(channel.Id, message.Id, attachmentId, callerId);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.Message.DeleteForbidden);
    }

    [Fact]
    public async Task HandleAsync_WhenAttachmentIsNotOnMessage_ShouldReturnAttachmentNotFound()
    {
        var channel = CreateChannel();
        var authorId = UserId.New();
        var attachmentId = UploadedFileId.New();
        var message = CreateMessage(channel.Id, authorId, UploadedFileId.New());

        _guildChannelRepositoryMock
            .Setup(x => x.GetWithCallerRoleAsync(channel.Id, authorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChannelAccessContext(channel, GuildRole.Member));

        _messageRepositoryMock
            .Setup(x => x.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var response = await _handler.HandleAsync(channel.Id, message.Id, attachmentId, authorId);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.Message.AttachmentNotFound);
    }

    [Fact]
    public async Task HandleAsync_WhenAuthorDeletesAttachment_ShouldRemoveReferenceCommitAndCleanupFile()
    {
        var channel = CreateChannel();
        var authorId = UserId.New();
        var attachmentId = UploadedFileId.New();
        var message = CreateMessage(channel.Id, authorId, attachmentId);
        var uploadedFile = CreateUploadedFile(attachmentId, authorId);
        var sequence = new MockSequence();

        _guildChannelRepositoryMock
            .InSequence(sequence)
            .Setup(x => x.GetWithCallerRoleAsync(channel.Id, authorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChannelAccessContext(channel, GuildRole.Member));

        _messageRepositoryMock
            .InSequence(sequence)
            .Setup(x => x.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        _unitOfWorkMock
            .InSequence(sequence)
            .Setup(x => x.BeginAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _messageRepositoryMock
            .InSequence(sequence)
            .Setup(x => x.UpdateAsync(
                It.Is<Message>(updatedMessage =>
                    updatedMessage.Id == message.Id
                    && updatedMessage.Attachments.All(attachment => attachment.FileId != attachmentId)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _messageRepositoryMock
            .InSequence(sequence)
            .Setup(x => x.RemoveAttachmentAsync(message.Id, attachmentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _transactionMock
            .InSequence(sequence)
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uploadedFileRepositoryMock
            .InSequence(sequence)
            .Setup(x => x.GetByIdAsync(attachmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadedFile);

        _objectStorageServiceMock
            .InSequence(sequence)
            .Setup(x => x.DeleteIfExistsAsync(uploadedFile.StorageKey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uploadedFileRepositoryMock
            .InSequence(sequence)
            .Setup(x => x.DeleteAsync(attachmentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _handler.HandleAsync(channel.Id, message.Id, attachmentId, authorId);

        response.Success.Should().BeTrue();
        message.Attachments.Should().BeEmpty();
        _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _messageRepositoryMock.Verify(
            x => x.RemoveAttachmentAsync(message.Id, attachmentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static GuildChannel CreateChannel(GuildChannelType type = GuildChannelType.Text)
    {
        return GuildChannel.Rehydrate(
            GuildChannelId.New(),
            GuildId.New(),
            "general",
            type,
            isDefault: true,
            position: 0,
            createdAtUtc: DateTime.UtcNow.AddDays(-2));
    }

    private static Message CreateMessage(
        GuildChannelId channelId,
        UserId authorId,
        UploadedFileId attachmentId)
    {
        var contentResult = MessageContent.Create("hello");
        if (contentResult.IsFailure || contentResult.Value is null)
            throw new InvalidOperationException("Failed to create message content for tests.");

        return Message.Rehydrate(
            MessageId.New(),
            channelId,
            conversationId: null,
            authorId,
            contentResult.Value,
            createdAtUtc: DateTime.UtcNow.AddMinutes(-5),
            updatedAtUtc: null,
            deletedAtUtc: null,
            attachments:
            [
                new MessageAttachment(attachmentId, "notes.txt", "text/plain", 12)
            ]);
    }

    private static UploadedFile CreateUploadedFile(
        UploadedFileId attachmentId,
        UserId uploaderUserId)
    {
        return UploadedFile.Rehydrate(
            attachmentId,
            uploaderUserId,
            "notes.txt",
            "text/plain",
            12,
            "attachments/file.txt",
            UploadPurpose.Attachment,
            DateTime.UtcNow.AddMinutes(-10));
    }
}
