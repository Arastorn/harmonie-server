using Harmonie.Application.Common;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Common.Messages;

/// <summary>
/// Scope-specific concerns for reaction operations (authorization and notification).
/// </summary>
public interface IReactionScope<TContext> where TContext : SendScopeContext
{
    Task<AuthorizationResult<TContext>> AuthorizeAsync(UserId caller, CancellationToken ct);
    Task NotifyReactionAddedAsync(TContext context, MessageId messageId, UserId userId, string emoji, CancellationToken ct);
    Task NotifyReactionRemovedAsync(TContext context, MessageId messageId, UserId userId, string emoji, CancellationToken ct);
}

/// <summary>
/// Result returned by <see cref="ReactionOrchestrator.GetUsersAsync"/>.
/// The caller maps this to the namespace-specific GetReactionUsersResponse DTO.
/// </summary>
public sealed record ReactionUsersResult(
    Guid MessageId,
    string Emoji,
    int TotalCount,
    IReadOnlyList<ReactionUserDto> Users,
    string? NextCursor);
