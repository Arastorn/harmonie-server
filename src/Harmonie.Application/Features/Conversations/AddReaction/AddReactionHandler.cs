using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Conversations.AddReaction;

public sealed record ConversationAddReactionInput(ConversationId ConversationId, MessageId MessageId, string Emoji);

public sealed class AddReactionHandler : IAuthenticatedHandler<ConversationAddReactionInput, bool>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IReactionNotifier _reactionNotifier;
    private readonly ILogger<ConversationReactionScope> _scopeLogger;
    private readonly ReactionOrchestrator _orchestrator;

    public AddReactionHandler(
        IConversationRepository conversationRepository,
        IReactionNotifier reactionNotifier,
        ILogger<ConversationReactionScope> scopeLogger,
        ReactionOrchestrator orchestrator)
    {
        _conversationRepository = conversationRepository;
        _reactionNotifier = reactionNotifier;
        _scopeLogger = scopeLogger;
        _orchestrator = orchestrator;
    }

    public async Task<ApplicationResponse<bool>> HandleAsync(
        ConversationAddReactionInput request,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = new ConversationReactionScope(
            request.ConversationId, _conversationRepository, _reactionNotifier, _scopeLogger);

        return await _orchestrator.AddAsync(
            scope,
            new MessageScope.Conversation(request.ConversationId),
            request.MessageId,
            request.Emoji,
            currentUserId,
            cancellationToken);
    }
}
