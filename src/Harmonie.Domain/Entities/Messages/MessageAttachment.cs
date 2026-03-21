using Harmonie.Domain.ValueObjects.Uploads;

namespace Harmonie.Domain.Entities.Messages;

public sealed record MessageAttachment(
    UploadedFileId FileId,
    string FileName,
    string ContentType,
    long SizeBytes);
