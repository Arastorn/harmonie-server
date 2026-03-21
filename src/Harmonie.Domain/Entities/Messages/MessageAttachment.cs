using Harmonie.Domain.ValueObjects;

namespace Harmonie.Domain.Entities;

public sealed record MessageAttachment(
    UploadedFileId FileId,
    string FileName,
    string ContentType,
    long SizeBytes);
