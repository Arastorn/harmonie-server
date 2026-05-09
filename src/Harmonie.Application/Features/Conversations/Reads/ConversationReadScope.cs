using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Conversations.Reads;

public sealed class ConversationReadScope : IReadScope<ConversationReadScope.Context>
{
    public sealed record Context : ScopeContext;

    private readonly ConversationId _conversationId;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationReadStateRepository _conversationReadStateRepository;

    public ConversationReadScope(
        ConversationId conversationId,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IConversationReadStateRepository conversationReadStateRepository)
    {
        _conversationId = conversationId;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _conversationReadStateRepository = conversationReadStateRepository;
    }

    public async Task<AuthorizationResult<Context>> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var result = await ConversationScopeAuthorizer.AuthorizeAsync(_conversationRepository, _conversationId, caller, ct);
        if (result is ConversationAuthResult.Denied denied)
            return new AuthorizationResult<Context>.Denied(denied.Error);

        return new AuthorizationResult<Context>.Authorized(new Context());
    }

    public Task<MessageId?> GetLatestMessageIdAsync(CancellationToken ct)
        => _messageRepository.GetLatestConversationMessageIdAsync(_conversationId, ct);

    public Task UpsertReadStateAsync(MessageReadState state, CancellationToken ct)
        => _conversationReadStateRepository.UpsertAsync(state, ct);
}
