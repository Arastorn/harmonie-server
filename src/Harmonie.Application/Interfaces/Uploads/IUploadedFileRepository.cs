using Harmonie.Domain.Entities.Uploads;
using Harmonie.Domain.ValueObjects.Uploads;

namespace Harmonie.Application.Interfaces.Uploads;

public interface IUploadedFileRepository
{
    Task<UploadedFile?> GetByIdAsync(
        UploadedFileId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UploadedFile>> GetByIdsAsync(
        IReadOnlyCollection<UploadedFileId> ids,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UploadedFile uploadedFile,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        UploadedFileId id,
        CancellationToken cancellationToken = default);
}
