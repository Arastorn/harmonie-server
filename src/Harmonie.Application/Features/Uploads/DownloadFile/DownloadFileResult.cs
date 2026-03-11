namespace Harmonie.Application.Features.Uploads.DownloadFile;

public sealed record DownloadFileResult(
    Stream Content,
    string ContentType,
    string FileName);
