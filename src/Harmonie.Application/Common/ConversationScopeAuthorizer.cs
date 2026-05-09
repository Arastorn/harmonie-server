using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Common;

/// <summary>
/// Discriminated union for conversation scope authorization results.
/// </summary>
public abstract record ConversationAuthResult
{
    private ConversationAuthResult() { }
    public sealed record Authorized(ConversationAccess Context) : ConversationAuthResult;
    public sealed record Denied(ApplicationError Error) : ConversationAuthResult;
}

/// <summary>
/// Shared authorization logic for conversation scopes. Eliminates the duplicated
/// GetByIdWithParticipantCheckAsync + 2 checks present in every conversation scope.
/// Each scope maps the returned <see cref="ConversationAccess"/> to its own Context type.
/// </summary>
public static class ConversationScopeAuthorizer
{
    public static async Task<ConversationAuthResult> AuthorizeAsync(
        IConversationRepository repository,
        ConversationId conversationId,
        UserId caller,
        CancellationToken ct)
    {
        var access = await repository.GetByIdWithParticipantCheckAsync(conversationId, caller, ct);
        if (access is null)
            return new ConversationAuthResult.Denied(
                new ApplicationError(ApplicationErrorCodes.Conversation.NotFound, "Conversation was not found"));

        if (access.Participant is null)
            return new ConversationAuthResult.Denied(
                new ApplicationError(ApplicationErrorCodes.Conversation.AccessDenied, "You do not have access to this conversation"));

        return new ConversationAuthResult.Authorized(access);
    }
}
