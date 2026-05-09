using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Conversations.Pins;

public sealed class ConversationPinnedMessageFetchScope : IPinnedMessageFetchScope<ConversationPinnedMessageFetchScope.Context>
{
    public sealed record Context : ScopeContext;

    private readonly ConversationId _conversationId;
    private readonly IConversationRepository _conversationRepository;
    private readonly IPinnedMessageRepository _pinnedMessageRepository;

    public ConversationPinnedMessageFetchScope(
        ConversationId conversationId,
        IConversationRepository conversationRepository,
        IPinnedMessageRepository pinnedMessageRepository)
    {
        _conversationId = conversationId;
        _conversationRepository = conversationRepository;
        _pinnedMessageRepository = pinnedMessageRepository;
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

    public Task<PinnedMessagesPage> GetPinnedPageAsync(
        UserId callerId, PinnedMessagesCursor? cursor, int limit, CancellationToken ct)
        => _pinnedMessageRepository.GetPinnedMessagesAsync(_conversationId, callerId, cursor, limit, ct);

    private static AuthorizationResult<Context> Denied(string code, string detail)
        => new AuthorizationResult<Context>.Denied(new ApplicationError(code, detail));
}
