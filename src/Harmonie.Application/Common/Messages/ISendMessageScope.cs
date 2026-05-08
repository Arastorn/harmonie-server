using Harmonie.Application.Common;
using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Common.Messages;

/// <summary>
/// Abstraction over scope-specific concerns for message operations
/// (authorization, notification, link previews, post-commit side effects).
/// </summary>
public interface ISendMessageScope
{
    /// <summary>
    /// Authorizes the caller for the scope.
    /// Returns an error on failure, null on success.
    /// </summary>
    Task<ApplicationError?> AuthorizeAsync(UserId caller, CancellationToken ct);

    /// <summary>
    /// Prepares post-commit side effects (e.g. unhiding participants).
    /// Must be called before the transaction is committed so the updates
    /// participate in the same unit of work.
    /// </summary>
    Task PreparePostCommitAsync(CancellationToken ct);

    /// <summary>
    /// Notifies scope participants that a message was created.
    /// Implementation must use best-effort notification (fire-and-forget).
    /// </summary>
    Task NotifyMessageCreatedAsync(
        Message message,
        IReadOnlyList<MessageAttachment> attachments,
        ReplyPreviewDto? replyTo,
        CancellationToken ct);

    /// <summary>
    /// Triggers fire-and-forget link preview resolution for the given message.
    /// </summary>
    void ResolveLinkPreviewsAsync(Message message, IReadOnlyList<Uri> urls, CancellationToken ct);
}

/// <summary>
/// Result returned by <see cref="MessageSendOrchestrator"/> when a message
/// is successfully sent. The caller uses this to build the scope-specific response DTO.
/// </summary>
public sealed record MessageSendResult(
    Guid MessageId,
    Guid AuthorUserId,
    string? Content,
    IReadOnlyList<MessageAttachmentDto> Attachments,
    ReplyPreviewDto? ReplyTo,
    DateTime CreatedAtUtc);
