using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Conversations.Messages;

public sealed class ConversationMessagePageScope : IMessagePageScope<ConversationMessagePageScope.Context>
{
    public sealed record Context : ScopeContext;

    private readonly ConversationId _conversationId;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessagePaginationRepository _paginationRepository;

    public ConversationMessagePageScope(
        ConversationId conversationId,
        IConversationRepository conversationRepository,
        IMessagePaginationRepository paginationRepository)
    {
        _conversationId = conversationId;
        _conversationRepository = conversationRepository;
        _paginationRepository = paginationRepository;
    }

    public async Task<AuthorizationResult<Context>> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var access = await _conversationRepository.GetByIdWithParticipantCheckAsync(_conversationId, caller, ct);
        if (access is null)
            return Denied(ApplicationErrorCodes.Conversation.NotFound, "Conversation was not found");

        if (access.Participant is null)
            return Denied(ApplicationErrorCodes.Conversation.AccessDenied, "You do not have access to this conversation");

        return new AuthorizationResult<Context>.Authorized(new Context());
    }

    public Task<MessagePage> GetPageAsync(MessageCursor? cursor, int limit, UserId callerId, CancellationToken ct)
        => _paginationRepository.GetConversationPageAsync(_conversationId, cursor, limit, callerId, ct);

    private static AuthorizationResult<Context> Denied(string code, string detail)
        => new AuthorizationResult<Context>.Denied(new ApplicationError(code, detail));
}
