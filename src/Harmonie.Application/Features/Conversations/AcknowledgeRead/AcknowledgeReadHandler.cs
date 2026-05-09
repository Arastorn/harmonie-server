using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Features.Conversations.Reads;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Conversations.AcknowledgeRead;

public sealed record AcknowledgeConversationReadInput(ConversationId ConversationId, MessageId? MessageId);

public sealed class AcknowledgeReadHandler : IAuthenticatedHandler<AcknowledgeConversationReadInput, bool>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationReadStateRepository _conversationReadStateRepository;
    private readonly ReadOrchestrator _orchestrator;

    public AcknowledgeReadHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IConversationReadStateRepository conversationReadStateRepository,
        ReadOrchestrator orchestrator)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _conversationReadStateRepository = conversationReadStateRepository;
        _orchestrator = orchestrator;
    }

    public async Task<ApplicationResponse<bool>> HandleAsync(
        AcknowledgeConversationReadInput request,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = new ConversationReadScope(
            request.ConversationId, _conversationRepository, _messageRepository, _conversationReadStateRepository);

        return await _orchestrator.AcknowledgeAsync(
            scope,
            new MessageScope.Conversation(request.ConversationId),
            request.MessageId,
            currentUserId,
            cancellationToken);
    }
}
