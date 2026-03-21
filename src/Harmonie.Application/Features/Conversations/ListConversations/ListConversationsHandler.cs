using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Conversations.ListConversations;

public sealed class ListConversationsHandler
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ILogger<ListConversationsHandler> _logger;

    public ListConversationsHandler(
        IConversationRepository conversationRepository,
        ILogger<ListConversationsHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _logger = logger;
    }

    public async Task<ApplicationResponse<ListConversationsResponse>> HandleAsync(
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "ListConversations started for user {UserId}",
            currentUserId);

        var conversations = await _conversationRepository.GetUserConversationsAsync(
            currentUserId,
            cancellationToken);

        var payload = new ListConversationsResponse(
            conversations.Select(conversation => new ListConversationsItemResponse(
                    ConversationId: conversation.ConversationId.ToString(),
                    OtherParticipantUserId: conversation.OtherParticipantUserId.ToString(),
                    OtherParticipantUsername: conversation.OtherParticipantUsername.Value,
                    CreatedAtUtc: conversation.CreatedAtUtc))
                .ToArray());

        _logger.LogInformation(
            "ListConversations succeeded for user {UserId}. ConversationCount={ConversationCount}",
            currentUserId,
            payload.Conversations.Count);

        return ApplicationResponse<ListConversationsResponse>.Ok(payload);
    }
}
