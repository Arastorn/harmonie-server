using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Common.Messages;

/// <summary>
/// Shared helpers for authorizing and fetching a message in orchestrators
/// that need both auth and message validation.
/// </summary>
public static class MessageScopeAuthorizer
{
    /// <summary>
    /// Authorizes the caller via the scope and fetches the target message,
    /// validating that it belongs to the expected scope.
    /// </summary>
    /// <param name="notFoundErrorCode">Error code to return when the message is not found
    /// (e.g. <see cref="ApplicationErrorCodes.Message.NotFound"/> or
    /// <see cref="ApplicationErrorCodes.Pin.MessageNotFound"/>).</param>
    public static async Task<FetchMessageResult<TContext>> AuthorizeAndFetchAsync<TContext>(
        IMessageRepository messageRepository,
        Func<UserId, CancellationToken, Task<AuthorizationResult<TContext>>> authorizeAsync,
        MessageScope messageScope,
        MessageId messageId,
        UserId callerId,
        string notFoundErrorCode,
        CancellationToken ct)
        where TContext : ScopeContext
    {
        var authResult = await authorizeAsync(callerId, ct);
        if (authResult is AuthorizationResult<TContext>.Denied denied)
            return new FetchMessageResult<TContext>.Failed(denied.Error);

        var context = ((AuthorizationResult<TContext>.Authorized)authResult).Context;

        var message = await messageRepository.GetByIdAsync(messageId, ct);
        if (message is null || message.Scope != messageScope)
        {
            return new FetchMessageResult<TContext>.Failed(new ApplicationError(
                notFoundErrorCode,
                "Message was not found"));
        }

        return new FetchMessageResult<TContext>.Found(context, message);
    }
}
