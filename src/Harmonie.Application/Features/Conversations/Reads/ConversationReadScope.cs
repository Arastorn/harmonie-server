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
        var access = await _conversationRepository.GetByIdWithParticipantCheckAsync(_conversationId, caller, ct);
        if (access is null)
            return Denied(ApplicationErrorCodes.Conversation.NotFound, "Conversation was not found");

        if (access.Participant is null)
            return Denied(ApplicationErrorCodes.Conversation.AccessDenied, "You do not have access to this conversation");

        return new AuthorizationResult<Context>.Authorized(new Context());
    }

    public Task<MessageId?> GetLatestMessageIdAsync(CancellationToken ct)
        => _messageRepository.GetLatestConversationMessageIdAsync(_conversationId, ct);

    public Task UpsertReadStateAsync(MessageReadState state, CancellationToken ct)
        => _conversationReadStateRepository.UpsertAsync(state, ct);

    private static AuthorizationResult<Context> Denied(string code, string detail)
        => new AuthorizationResult<Context>.Denied(new ApplicationError(code, detail));
}
