using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Conversations.GetConversationParticipants;

public sealed class GetConversationParticipantsHandler
    : IAuthenticatedHandler<ConversationId, GetConversationParticipantsResponse>
{
    private readonly IConversationRepository _conversationRepository;

    public GetConversationParticipantsHandler(
        IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task<ApplicationResponse<GetConversationParticipantsResponse>> HandleAsync(
        ConversationId conversationId,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await _conversationRepository.GetParticipantsWithProfilesAsync(
            conversationId, currentUserId, cancellationToken);

        if (access is null)
        {
            return ApplicationResponse<GetConversationParticipantsResponse>.Fail(
                ApplicationErrorCodes.Conversation.NotFound,
                "Conversation was not found");
        }

        if (access.CallerParticipant is null)
        {
            return ApplicationResponse<GetConversationParticipantsResponse>.Fail(
                ApplicationErrorCodes.Conversation.AccessDenied,
                "You do not have access to this conversation");
        }

        var dtos = access.Participants
            .Select(p => new ConversationParticipantDto(
                UserId: p.UserId,
                Username: p.Username,
                DisplayName: p.DisplayName,
                AvatarFileId: p.AvatarFileId,
                AvatarColor: p.AvatarColor,
                AvatarIcon: p.AvatarIcon,
                AvatarBg: p.AvatarBg,
                JoinedAtUtc: p.JoinedAtUtc))
            .ToArray();

        return ApplicationResponse<GetConversationParticipantsResponse>.Ok(
            new GetConversationParticipantsResponse(dtos));
    }
}
