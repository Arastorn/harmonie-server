using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Uploads;
using Harmonie.Domain.ValueObjects.Uploads;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Uploads.DownloadFile;

public sealed class DownloadFileHandler
{
    private readonly IUploadedFileRepository _uploadedFileRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<DownloadFileHandler> _logger;

    public DownloadFileHandler(
        IUploadedFileRepository uploadedFileRepository,
        IObjectStorageService objectStorageService,
        ILogger<DownloadFileHandler> logger)
    {
        _uploadedFileRepository = uploadedFileRepository;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<ApplicationResponse<DownloadFileResult>> HandleAsync(
        UploadedFileId fileId,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DownloadFile started. FileId={FileId}, UserId={UserId}",
            fileId,
            currentUserId);

        var uploadedFile = await _uploadedFileRepository.GetByIdAsync(fileId, cancellationToken);
        if (uploadedFile is null)
        {
            _logger.LogWarning(
                "DownloadFile failed because file was not found. FileId={FileId}",
                fileId);

            return ApplicationResponse<DownloadFileResult>.Fail(
                ApplicationErrorCodes.Upload.NotFound,
                "File was not found");
        }

        var stream = await _objectStorageService.GetStreamAsync(
            uploadedFile.StorageKey,
            cancellationToken);

        if (stream is null)
        {
            _logger.LogWarning(
                "DownloadFile failed because file content is unavailable. FileId={FileId}, StorageKey={StorageKey}",
                fileId,
                uploadedFile.StorageKey);

            return ApplicationResponse<DownloadFileResult>.Fail(
                ApplicationErrorCodes.Upload.StorageUnavailable,
                "File content is unavailable");
        }

        _logger.LogInformation(
            "DownloadFile succeeded. FileId={FileId}, UserId={UserId}",
            fileId,
            currentUserId);

        return ApplicationResponse<DownloadFileResult>.Ok(
            new DownloadFileResult(
                stream,
                uploadedFile.ContentType,
                uploadedFile.FileName));
    }
}
