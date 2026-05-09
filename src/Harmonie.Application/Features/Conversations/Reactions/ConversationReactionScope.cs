using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.Entities.Conversations;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Conversations.Reactions;

public sealed class ConversationReactionScope : IReactionScope<ConversationReactionScope.Context>
{
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(5);

    public sealed record Context(
        ConversationId ConversationId,
        string? ConversationName,
        ConversationType ConversationType,
        string CallerUsername,
        string CallerDisplayName) : ScopeContext;

    private readonly ConversationId _conversationId;
    private readonly IConversationRepository _conversationRepository;
    private readonly IReactionNotifier _reactionNotifier;
    private readonly ILogger<ConversationReactionScope> _logger;

    public ConversationReactionScope(
        ConversationId conversationId,
        IConversationRepository conversationRepository,
        IReactionNotifier reactionNotifier,
        ILogger<ConversationReactionScope> logger)
    {
        _conversationId = conversationId;
        _conversationRepository = conversationRepository;
        _reactionNotifier = reactionNotifier;
        _logger = logger;
    }

    public async Task<AuthorizationResult<Context>> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var result = await ConversationScopeAuthorizer.AuthorizeAsync(_conversationRepository, _conversationId, caller, ct);
        if (result is ConversationAuthResult.Denied denied)
            return new AuthorizationResult<Context>.Denied(denied.Error);

        var access = ((ConversationAuthResult.Authorized)result).Context;
        return new AuthorizationResult<Context>.Authorized(new Context(
            _conversationId, access.Conversation.Name, access.Conversation.Type,
            access.CallerUsername ?? string.Empty,
            access.CallerDisplayName ?? string.Empty));
    }

    public async Task NotifyReactionAddedAsync(
        Context context, MessageId messageId, UserId userId, string emoji, CancellationToken ct)
    {
        var notification = new ConversationReactionAddedNotification(
            messageId, context.ConversationId, context.ConversationName,
            context.ConversationType.ToString(),
            userId, context.CallerUsername, context.CallerDisplayName, emoji);

        await BestEffortNotificationHelper.TryNotifyAsync(
            token => _reactionNotifier.NotifyReactionAddedToConversationAsync(notification, token),
            NotificationTimeout, _logger,
            "AddConversationReaction notification failed (best-effort). MessageId={MessageId}, ConversationId={ConversationId}",
            notification.MessageId, notification.ConversationId);
    }

    public async Task NotifyReactionRemovedAsync(
        Context context, MessageId messageId, UserId userId, string emoji, CancellationToken ct)
    {
        var notification = new ConversationReactionRemovedNotification(
            messageId, context.ConversationId, context.ConversationName,
            context.ConversationType.ToString(),
            userId, context.CallerUsername, context.CallerDisplayName, emoji);

        await BestEffortNotificationHelper.TryNotifyAsync(
            token => _reactionNotifier.NotifyReactionRemovedFromConversationAsync(notification, token),
            NotificationTimeout, _logger,
            "RemoveConversationReaction notification failed (best-effort). MessageId={MessageId}, ConversationId={ConversationId}",
            notification.MessageId, notification.ConversationId);
    }
}
