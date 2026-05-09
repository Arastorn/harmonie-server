using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Features.Conversations.Pins;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Conversations.UnpinMessage;

public sealed record ConversationUnpinMessageInput(ConversationId ConversationId, MessageId MessageId);

public sealed class UnpinMessageHandler : IAuthenticatedHandler<ConversationUnpinMessageInput, bool>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IPinNotifier _pinNotifier;
    private readonly ILogger<ConversationPinScope> _scopeLogger;
    private readonly PinOrchestrator _orchestrator;

    public UnpinMessageHandler(
        IConversationRepository conversationRepository,
        IPinNotifier pinNotifier,
        ILogger<ConversationPinScope> scopeLogger,
        PinOrchestrator orchestrator)
    {
        _conversationRepository = conversationRepository;
        _pinNotifier = pinNotifier;
        _scopeLogger = scopeLogger;
        _orchestrator = orchestrator;
    }

    public async Task<ApplicationResponse<bool>> HandleAsync(
        ConversationUnpinMessageInput request,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = new ConversationPinScope(
            request.ConversationId, _conversationRepository, _pinNotifier, _scopeLogger);

        return await _orchestrator.UnpinAsync(
            scope,
            new MessageScope.Conversation(request.ConversationId),
            request.MessageId,
            currentUserId,
            cancellationToken);
    }
}
