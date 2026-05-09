using Harmonie.Application.Common;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Common.Messages;

/// <summary>
/// Scope-specific concerns for pin/unpin operations (authorization and notification).
/// </summary>
public interface IPinScope<TContext> where TContext : ScopeContext
{
    Task<AuthorizationResult<TContext>> AuthorizeAsync(UserId caller, CancellationToken ct);

    Task NotifyPinAddedAsync(TContext context, MessageId messageId, UserId userId, DateTime pinnedAtUtc, CancellationToken ct);

    Task NotifyPinRemovedAsync(TContext context, MessageId messageId, UserId userId, DateTime unpinnedAtUtc, CancellationToken ct);
}
