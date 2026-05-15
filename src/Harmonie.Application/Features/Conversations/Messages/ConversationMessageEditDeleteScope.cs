using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.Common;
using Harmonie.Domain.Entities.Conversations;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Conversations.Messages;

/// <summary>
/// Conversation-specific implementation of <see cref="IMessageEditDeleteScope{TContext}"/>.
/// </summary>
public sealed class ConversationMessageEditDeleteScope : IMessageEditDeleteScope<ConversationMessageEditDeleteScope.Context>
{
    public sealed record Context(
        ConversationId ConversationId,
        string? ConversationName,
        ConversationType ConversationType,
        IReadOnlyList<ConversationParticipant> AllParticipants) : ScopeContext;

    private readonly ConversationId _conversationId;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageEventPublisher _messageEventPublisher;
    private readonly ILogger<ConversationMessageEditDeleteScope> _logger;

    public ConversationMessageEditDeleteScope(
        ConversationId conversationId,
        IConversationRepository conversationRepository,
        IMessageEventPublisher messageEventPublisher,
        ILogger<ConversationMessageEditDeleteScope> logger)
    {
        _conversationId = conversationId;
        _conversationRepository = conversationRepository;
        _messageEventPublisher = messageEventPublisher;
        _logger = logger;
    }

    public async Task<AuthorizationResult<Context>> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var access = await _conversationRepository.GetByIdWithAllParticipantsAsync(_conversationId, caller, ct);
        if (access is null)
        {
            return new AuthorizationResult<Context>.Denied(new ApplicationError(
                ApplicationErrorCodes.Conversation.NotFound,
                "Conversation was not found"));
        }
        if (access.CallerParticipant is null)
        {
            return new AuthorizationResult<Context>.Denied(new ApplicationError(
                ApplicationErrorCodes.Conversation.AccessDenied,
                "You do not have access to this conversation"));
        }

        return new AuthorizationResult<Context>.Authorized(new Context(
            _conversationId,
            access.Conversation.Name,
            access.Conversation.Type,
            access.AllParticipants));
    }

    // Conversations have no admin role; only the author can delete their own messages.
    public bool CanDeleteOthersMessages(Context context)
        => false;

    public Task<Result> ValidateMentionedUsersAsync(
        IReadOnlyCollection<UserId> userIds,
        Context context,
        CancellationToken ct)
    {
        var participantIds = context.AllParticipants.Select(p => p.UserId).ToHashSet();
        foreach (var userId in userIds)
        {
            if (!participantIds.Contains(userId))
            {
                return Task.FromResult(Result.Failure($"User {userId.Value} is not a participant of conversation {context.ConversationId.Value}"));
            }
        }

        return Task.FromResult(Result.Success());
    }

    public async Task NotifyMessageUpdatedAsync(
        Context context,
        MessageId messageId,
        string? content,
        IReadOnlyList<Guid> mentionedUserIds,
        DateTime updatedAtUtc,
        CancellationToken ct)
    {
        await _messageEventPublisher.PublishUpdatedAsync(
            new MessageUpdatedEventEnvelope(
                messageId,
                new MessageEventLocation.Conversation(
                    context.ConversationId,
                    context.ConversationName,
                    context.ConversationType.ToString()),
                content,
                mentionedUserIds,
                updatedAtUtc),
            CancellationToken.None);
    }

    public async Task NotifyMessageDeletedAsync(
        Context context,
        MessageId messageId,
        CancellationToken ct)
    {
        await _messageEventPublisher.PublishDeletedAsync(
            new MessageDeletedEventEnvelope(
                messageId,
                new MessageEventLocation.Conversation(
                    context.ConversationId,
                    context.ConversationName,
                    context.ConversationType.ToString())),
            CancellationToken.None);
    }
}
