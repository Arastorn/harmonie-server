using Harmonie.Domain.Entities.Messages;

namespace Harmonie.Application.Common.Messages;

public sealed record MessageAttachmentDto(
    string FileId,
    string FileName,
    string ContentType,
    long SizeBytes)
{
    public static MessageAttachmentDto FromDomain(MessageAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        return new MessageAttachmentDto(
            attachment.FileId.ToString(),
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeBytes);
    }
}
