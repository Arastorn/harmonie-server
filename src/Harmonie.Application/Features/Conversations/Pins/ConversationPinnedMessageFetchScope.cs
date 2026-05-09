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
        var result = await ConversationScopeAuthorizer.AuthorizeAsync(_conversationRepository, _conversationId, caller, ct);
        if (result is ConversationAuthResult.Denied denied)
            return new AuthorizationResult<Context>.Denied(denied.Error);

        return new AuthorizationResult<Context>.Authorized(new Context());
    }

    public Task<PinnedMessagesPage> GetPinnedPageAsync(
        UserId callerId, PinnedMessagesCursor? cursor, int limit, CancellationToken ct)
        => _pinnedMessageRepository.GetPinnedMessagesAsync(_conversationId, callerId, cursor, limit, ct);
}
