using Harmonie.Application.Common;
using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Common.Messages;

/// <summary>
/// Scope-specific concerns for read acknowledgement operations (authorization,
/// latest message resolution, and read state persistence).
/// </summary>
public interface IReadScope<TContext> where TContext : ScopeContext
{
    Task<AuthorizationResult<TContext>> AuthorizeAsync(UserId caller, CancellationToken ct);
    Task<MessageId?> GetLatestMessageIdAsync(CancellationToken ct);
    Task UpsertReadStateAsync(MessageReadState state, CancellationToken ct);
}
