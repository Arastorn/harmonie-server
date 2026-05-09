using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Common.Messages;

/// <summary>
/// Scope-specific concerns for message pagination (authorization and page fetching).
/// Used by GetMessages where GetChannelPageAsync vs GetConversationPageAsync differ.
/// </summary>
public interface IMessagePageScope<TContext> where TContext : ScopeContext
{
    Task<AuthorizationResult<TContext>> AuthorizeAsync(UserId caller, CancellationToken ct);
    Task<MessagePage> GetPageAsync(MessageCursor? cursor, int limit, UserId callerId, CancellationToken ct);
}
