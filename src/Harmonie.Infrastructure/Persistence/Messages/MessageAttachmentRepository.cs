using Dapper;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Uploads;
using Harmonie.Infrastructure.Persistence.Common;
using Harmonie.Infrastructure.Rows.Messages;

namespace Harmonie.Infrastructure.Persistence.Messages;

internal sealed class MessageAttachmentRepository : IMessageAttachmentRepository
{
    private readonly DbSession _dbSession;

    public MessageAttachmentRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<IReadOnlyDictionary<MessageId, IReadOnlyList<MessageAttachment>>> GetByMessageIdsAsync(
        IReadOnlyCollection<MessageId> messageIds,
        CancellationToken cancellationToken = default)
    {
        if (messageIds.Count == 0)
            return new Dictionary<MessageId, IReadOnlyList<MessageAttachment>>();

        const string sql = """
                           SELECT ma.message_id AS "MessageId",
                                  ma.position AS "Position",
                                  uf.id AS "UploadedFileId",
                                  uf.filename AS "FileName",
                                  uf.content_type AS "ContentType",
                                  uf.size_bytes AS "SizeBytes"
                           FROM message_attachments ma
                           INNER JOIN uploaded_files uf ON uf.id = ma.uploaded_file_id
                           WHERE ma.message_id = ANY(@MessageIds)
                           ORDER BY ma.message_id, ma.position
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { MessageIds = messageIds.Select(id => id.Value).ToArray() },
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<MessageAttachmentRow>(command);

        return rows
            .GroupBy(row => row.MessageId)
            .ToDictionary(
                group => MessageId.From(group.Key),
                group => (IReadOnlyList<MessageAttachment>)group
                    .OrderBy(row => row.Position)
                    .Select(MapRow)
                    .ToArray());
    }

    public async Task<IReadOnlyList<MessageAttachment>> GetByMessageIdAsync(
        MessageId messageId,
        CancellationToken cancellationToken = default)
    {
        var byMessageId = await GetByMessageIdsAsync([messageId], cancellationToken);
        return byMessageId.TryGetValue(messageId, out var attachments)
            ? attachments
            : Array.Empty<MessageAttachment>();
    }

    public async Task AddRangeAsync(
        IEnumerable<MessageAttachment> attachments,
        CancellationToken cancellationToken = default)
    {
        var attachmentList = attachments as IReadOnlyCollection<MessageAttachment> ?? attachments.ToArray();
        if (attachmentList.Count == 0)
            return;

        const string sql = """
                           INSERT INTO message_attachments (
                               message_id,
                               uploaded_file_id,
                               position)
                           VALUES (
                               @MessageId,
                               @UploadedFileId,
                               @Position)
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            attachmentList.Select(attachment => new
            {
                MessageId = attachment.MessageId.Value,
                UploadedFileId = attachment.FileId.Value,
                attachment.Position
            }),
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public async Task<bool> DeleteAsync(
        MessageId messageId,
        UploadedFileId fileId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           DELETE FROM message_attachments
                           WHERE message_id = @MessageId
                             AND uploaded_file_id = @UploadedFileId
                           """;

        var connection = await _dbSession.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                MessageId = messageId.Value,
                UploadedFileId = fileId.Value
            },
            transaction: _dbSession.Transaction,
            cancellationToken: cancellationToken);

        var rowsAffected = await connection.ExecuteAsync(command);
        return rowsAffected > 0;
    }

    internal static MessageAttachment MapRow(MessageAttachmentRow row)
    {
        return MessageAttachment.Rehydrate(
            MessageId.From(row.MessageId),
            UploadedFileId.From(row.UploadedFileId),
            row.FileName,
            row.ContentType,
            row.SizeBytes,
            row.Position);
    }

    internal static IReadOnlyDictionary<Guid, IReadOnlyList<MessageAttachment>> BuildByMessageIdDictionary(
        IEnumerable<MessageAttachmentRow> rows)
    {
        return rows
            .GroupBy(row => row.MessageId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MessageAttachment>)group
                    .OrderBy(row => row.Position)
                    .Select(MapRow)
                    .ToArray());
    }
}
