using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Uploads;

namespace Harmonie.Application.Interfaces.Messages;

public interface IMessageAttachmentRepository
{
    Task<IReadOnlyDictionary<MessageId, IReadOnlyList<MessageAttachment>>> GetByMessageIdsAsync(
        IReadOnlyCollection<MessageId> messageIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageAttachment>> GetByMessageIdAsync(
        MessageId messageId,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<MessageAttachment> attachments,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        MessageId messageId,
        UploadedFileId fileId,
        CancellationToken cancellationToken = default);
}
