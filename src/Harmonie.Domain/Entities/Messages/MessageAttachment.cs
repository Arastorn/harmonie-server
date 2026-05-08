using Harmonie.Domain.Common;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Uploads;

namespace Harmonie.Domain.Entities.Messages;

public sealed class MessageAttachment
{
    public MessageId MessageId { get; }

    public UploadedFileId FileId { get; }

    public string FileName { get; }

    public string ContentType { get; }

    public long SizeBytes { get; }

    public int Position { get; }

    private MessageAttachment(
        MessageId messageId,
        UploadedFileId fileId,
        string fileName,
        string contentType,
        long sizeBytes,
        int position)
    {
        MessageId = messageId;
        FileId = fileId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Position = position;
    }

    public static Result<MessageAttachment> Create(
        MessageId messageId,
        UploadedFileId fileId,
        string fileName,
        string contentType,
        long sizeBytes,
        int position)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result.Failure<MessageAttachment>("Attachment file name is required");

        if (string.IsNullOrWhiteSpace(contentType))
            return Result.Failure<MessageAttachment>("Attachment content type is required");

        if (sizeBytes <= 0)
            return Result.Failure<MessageAttachment>("Attachment size must be greater than zero");

        if (position < 0)
            return Result.Failure<MessageAttachment>("Attachment position cannot be negative");

        return Result.Success(new MessageAttachment(
            messageId,
            fileId,
            fileName,
            contentType,
            sizeBytes,
            position));
    }

    public static MessageAttachment Rehydrate(
        MessageId messageId,
        UploadedFileId fileId,
        string fileName,
        string contentType,
        long sizeBytes,
        int position)
    {
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentNullException.ThrowIfNull(fileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (sizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Attachment size must be greater than zero.");

        if (position < 0)
            throw new ArgumentOutOfRangeException(nameof(position), "Attachment position cannot be negative.");

        return new MessageAttachment(messageId, fileId, fileName, contentType, sizeBytes, position);
    }
}
