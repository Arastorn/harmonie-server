using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.Entities.Conversations;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Conversations.Pins;

public sealed class ConversationPinScope : IPinScope<ConversationPinScope.Context>
{
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(5);

    public sealed record Context(
        ConversationId ConversationId,
        string? ConversationName,
        ConversationType ConversationType,
        string CallerUsername,
        string? CallerDisplayName) : ScopeContext;

    private readonly ConversationId _conversationId;
    private readonly IConversationRepository _conversationRepository;
    private readonly IPinNotifier _pinNotifier;
    private readonly ILogger<ConversationPinScope> _logger;

    public ConversationPinScope(
        ConversationId conversationId,
        IConversationRepository conversationRepository,
        IPinNotifier pinNotifier,
        ILogger<ConversationPinScope> logger)
    {
        _conversationId = conversationId;
        _conversationRepository = conversationRepository;
        _pinNotifier = pinNotifier;
        _logger = logger;
    }

    public async Task<AuthorizationResult<Context>> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var access = await _conversationRepository.GetByIdWithParticipantCheckAsync(_conversationId, caller, ct);
        if (access is null)
            return Denied(ApplicationErrorCodes.Conversation.NotFound, "Conversation was not found");

        if (access.Participant is null)
            return Denied(ApplicationErrorCodes.Conversation.AccessDenied, "You do not have access to this conversation");

        return new AuthorizationResult<Context>.Authorized(new Context(
            _conversationId, access.Conversation.Name, access.Conversation.Type,
            access.CallerUsername ?? string.Empty,
            access.CallerDisplayName));
    }

    public async Task NotifyPinAddedAsync(
        Context context, MessageId messageId, UserId userId, DateTime pinnedAtUtc, CancellationToken ct)
    {
        var notification = new ConversationPinAddedNotification(
            messageId, context.ConversationId, context.ConversationName,
            context.ConversationType.ToString(),
            userId, context.CallerUsername, context.CallerDisplayName, pinnedAtUtc);

        await BestEffortNotificationHelper.TryNotifyAsync(
            token => _pinNotifier.NotifyMessagePinnedInConversationAsync(notification, token),
            NotificationTimeout, _logger,
            "Conversation pin notification failed (best-effort). MessageId={MessageId}, ConversationId={ConversationId}",
            messageId, context.ConversationId);
    }

    public async Task NotifyPinRemovedAsync(
        Context context, MessageId messageId, UserId userId, DateTime unpinnedAtUtc, CancellationToken ct)
    {
        var notification = new ConversationPinRemovedNotification(
            messageId, context.ConversationId, context.ConversationName,
            context.ConversationType.ToString(),
            userId, context.CallerUsername, context.CallerDisplayName, unpinnedAtUtc);

        await BestEffortNotificationHelper.TryNotifyAsync(
            token => _pinNotifier.NotifyMessageUnpinnedInConversationAsync(notification, token),
            NotificationTimeout, _logger,
            "Conversation unpin notification failed (best-effort). MessageId={MessageId}, ConversationId={ConversationId}",
            messageId, context.ConversationId);
    }

    private static AuthorizationResult<Context> Denied(string code, string detail)
        => new AuthorizationResult<Context>.Denied(new ApplicationError(code, detail));
}
