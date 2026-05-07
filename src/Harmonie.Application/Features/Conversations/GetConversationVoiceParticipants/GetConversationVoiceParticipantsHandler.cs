using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Voice;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Conversations.GetConversationVoiceParticipants;

public sealed class GetConversationVoiceParticipantsHandler
    : IAuthenticatedHandler<ConversationId, GetConversationVoiceParticipantsResponse>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ILiveKitRoomService _liveKitRoomService;
    private readonly IConversationVoiceParticipantCache _voiceParticipantCache;

    public GetConversationVoiceParticipantsHandler(
        IConversationRepository conversationRepository,
        ILiveKitRoomService liveKitRoomService,
        IConversationVoiceParticipantCache voiceParticipantCache)
    {
        _conversationRepository = conversationRepository;
        _liveKitRoomService = liveKitRoomService;
        _voiceParticipantCache = voiceParticipantCache;
    }

    public async Task<ApplicationResponse<GetConversationVoiceParticipantsResponse>> HandleAsync(
        ConversationId request,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await _conversationRepository.GetByIdWithParticipantCheckAsync(request, currentUserId, cancellationToken);
        if (access is null)
        {
            return ApplicationResponse<GetConversationVoiceParticipantsResponse>.Fail(
                ApplicationErrorCodes.Conversation.NotFound,
                "Conversation was not found");
        }

        if (access.Participant is null)
        {
            return ApplicationResponse<GetConversationVoiceParticipantsResponse>.Fail(
                ApplicationErrorCodes.Conversation.VoiceAccessDenied,
                "You do not have access to this conversation");
        }

        var liveKitParticipantsTask = _liveKitRoomService.ListConversationParticipantsAsync(request, cancellationToken);
        var cachedParticipantsTask = _voiceParticipantCache.GetAsync(request, cancellationToken);

        await Task.WhenAll(liveKitParticipantsTask, cachedParticipantsTask);

        var liveKitParticipants = liveKitParticipantsTask.Result;
        var cachedParticipants = cachedParticipantsTask.Result;

        var cachedById = cachedParticipants.ToDictionary(p => p.UserId.Value);

        var participants = liveKitParticipants
            .Select(lkParticipant =>
            {
                cachedById.TryGetValue(lkParticipant.UserId.Value, out var cached);
                return new ConversationVoiceParticipantDto(
                    UserId: lkParticipant.UserId.Value,
                    Username: cached?.Username ?? lkParticipant.Username,
                    DisplayName: cached?.DisplayName,
                    AvatarFileId: cached?.AvatarFileId?.Value,
                    AvatarColor: cached?.AvatarColor,
                    AvatarIcon: cached?.AvatarIcon,
                    AvatarBg: cached?.AvatarBg,
                    IsSharingScreen: lkParticipant.IsSharingScreen);
            })
            .ToArray();

        return ApplicationResponse<GetConversationVoiceParticipantsResponse>.Ok(
            new GetConversationVoiceParticipantsResponse(participants));
    }
}
