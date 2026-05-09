using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Common.Messages;

/// <summary>
/// Scope-specific concerns for listing pinned messages (authorization and page fetch).
/// </summary>
public interface IPinnedMessageFetchScope<TContext> where TContext : ScopeContext
{
    Task<AuthorizationResult<TContext>> AuthorizeAsync(UserId caller, CancellationToken ct);
    Task<PinnedMessagesPage> GetPinnedPageAsync(UserId callerId, PinnedMessagesCursor? cursor, int limit, CancellationToken ct);
}
