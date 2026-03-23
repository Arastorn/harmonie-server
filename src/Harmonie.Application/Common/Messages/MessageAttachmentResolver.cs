using Harmonie.Application.Interfaces.Uploads;
using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Uploads;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Common.Messages;

public sealed class MessageAttachmentResolver
{
    private readonly IUploadedFileRepository _uploadedFileRepository;

    public MessageAttachmentResolver(IUploadedFileRepository uploadedFileRepository)
    {
        _uploadedFileRepository = uploadedFileRepository;
    }

    public async Task<MessageAttachmentResolutionResult> ResolveAsync(
        IReadOnlyList<Guid>? attachmentFileIds,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (attachmentFileIds is null || attachmentFileIds.Count == 0)
            return MessageAttachmentResolutionResult.Succeeded(Array.Empty<MessageAttachment>());

        var parsedIds = new List<UploadedFileId>(attachmentFileIds.Count);
        var seenIds = new HashSet<Guid>();

        foreach (var attachmentFileId in attachmentFileIds)
        {
            if (!seenIds.Add(attachmentFileId))
            {
                return MessageAttachmentResolutionResult.Failed(
                    "Attachment file IDs must be valid, non-empty, and unique.");
            }

            parsedIds.Add(UploadedFileId.From(attachmentFileId));
        }

        var uploadedFiles = await _uploadedFileRepository.GetByIdsAsync(parsedIds, cancellationToken);
        if (uploadedFiles.Count != parsedIds.Count)
        {
            return MessageAttachmentResolutionResult.Failed(
                "One or more attachment files were not found.");
        }

        var uploadedFilesById = uploadedFiles.ToDictionary(file => file.Id.Value);
        var attachments = new List<MessageAttachment>(parsedIds.Count);

        foreach (var parsedId in parsedIds)
        {
            if (!uploadedFilesById.TryGetValue(parsedId.Value, out var uploadedFile))
            {
                return MessageAttachmentResolutionResult.Failed(
                    "One or more attachment files were not found.");
            }

            if (uploadedFile.UploaderUserId != currentUserId)
            {
                return MessageAttachmentResolutionResult.Failed(
                    "Attachments must belong to the authenticated user.");
            }

            if (uploadedFile.Purpose != UploadPurpose.Attachment)
            {
                return MessageAttachmentResolutionResult.Failed(
                    "Only files uploaded with attachment purpose can be attached to messages.");
            }

            attachments.Add(new MessageAttachment(
                uploadedFile.Id,
                uploadedFile.FileName,
                uploadedFile.ContentType,
                uploadedFile.SizeBytes));
        }

        return MessageAttachmentResolutionResult.Succeeded(attachments);
    }
}

public sealed record MessageAttachmentResolutionResult(
    bool Success,
    IReadOnlyList<MessageAttachment> Attachments,
    string? Error)
{
    public static MessageAttachmentResolutionResult Succeeded(IReadOnlyList<MessageAttachment> attachments)
        => new(true, attachments, null);

    public static MessageAttachmentResolutionResult Failed(string error)
        => new(false, Array.Empty<MessageAttachment>(), error);
}
