using Harmonie.Application.Common;
using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Common.Messages;

/// <summary>
/// Opaque context returned by <see cref="ISendMessageScope.AuthorizeAsync"/>
/// and consumed by downstream scope methods. Concrete types are internal to
/// each scope implementation.
/// </summary>
public abstract record SendScopeContext
{
    private protected SendScopeContext() { }
}

/// <summary>
/// Abstraction over scope-specific concerns for message operations
/// (authorization, notification, link previews, in-transaction side effects).
/// </summary>
public interface ISendMessageScope
{
    /// <summary>
    /// Authorizes the caller for the scope.
    /// Returns an error on failure, or a context on success.
    /// </summary>
    Task<AuthorizationResult> AuthorizeAsync(UserId caller, CancellationToken ct);

    /// <summary>
    /// Applies scope-specific side effects that must participate in the same
    /// unit of work (e.g. unhiding participants on send).
    /// Called inside the transaction, before commit.
    /// </summary>
    Task ApplyInTransactionSideEffectsAsync(SendScopeContext context, CancellationToken ct);

    /// <summary>
    /// Notifies scope participants that a message was created.
    /// Implementation must use best-effort notification (fire-and-forget).
    /// </summary>
    Task NotifyMessageCreatedAsync(
        SendScopeContext context,
        Message message,
        IReadOnlyList<MessageAttachmentDto> attachments,
        ReplyPreviewDto? replyTo,
        CancellationToken ct);

    /// <summary>
    /// Triggers fire-and-forget link preview resolution for the given message.
    /// </summary>
    void ScheduleLinkPreviewResolution(
        SendScopeContext context,
        Message message,
        IReadOnlyList<Uri> urls,
        CancellationToken ct);
}

/// <summary>
/// Result of <see cref="ISendMessageScope.AuthorizeAsync"/>.
/// </summary>
public sealed record AuthorizationResult(
    SendScopeContext? Context,
    ApplicationError? Error)
{
    public bool IsAuthorized => Context is not null && Error is null;
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
