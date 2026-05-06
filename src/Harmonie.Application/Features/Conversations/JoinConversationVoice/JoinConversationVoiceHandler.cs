using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Users;
using Harmonie.Application.Interfaces.Voice;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Conversations.JoinConversationVoice;

public sealed class JoinConversationVoiceHandler : IAuthenticatedHandler<ConversationId, JoinConversationVoiceResponse>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILiveKitTokenService _liveKitTokenService;
    private readonly ILiveKitRoomService _liveKitRoomService;
    private readonly IConversationVoiceParticipantCache _voiceParticipantCache;

    public JoinConversationVoiceHandler(
        IConversationRepository conversationRepository,
        IUserRepository userRepository,
        ILiveKitTokenService liveKitTokenService,
        ILiveKitRoomService liveKitRoomService,
        IConversationVoiceParticipantCache voiceParticipantCache)
    {
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
        _liveKitTokenService = liveKitTokenService;
        _liveKitRoomService = liveKitRoomService;
        _voiceParticipantCache = voiceParticipantCache;
    }

    public async Task<ApplicationResponse<JoinConversationVoiceResponse>> HandleAsync(
        ConversationId request,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await _conversationRepository.GetByIdWithParticipantCheckAsync(request, currentUserId, cancellationToken);
        if (access is null)
        {
            return ApplicationResponse<JoinConversationVoiceResponse>.Fail(
                ApplicationErrorCodes.Conversation.NotFound,
                "Conversation was not found");
        }

        if (access.Participant is null)
        {
            return ApplicationResponse<JoinConversationVoiceResponse>.Fail(
                ApplicationErrorCodes.Conversation.VoiceAccessDenied,
                "You do not have access to this conversation");
        }

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (user is null)
        {
            return ApplicationResponse<JoinConversationVoiceResponse>.Fail(
                ApplicationErrorCodes.User.NotFound,
                "User profile was not found");
        }

        var roomTokenTask = _liveKitTokenService.GenerateConversationRoomTokenAsync(
            request,
            currentUserId,
            user.Username.Value,
            cancellationToken);
        var liveKitParticipantsTask = _liveKitRoomService.ListConversationParticipantsAsync(request, cancellationToken);
        var cachedParticipantsTask = _voiceParticipantCache.GetAsync(request, cancellationToken);

        await Task.WhenAll(roomTokenTask, liveKitParticipantsTask, cachedParticipantsTask);

        var roomToken = roomTokenTask.Result;
        var liveKitParticipants = liveKitParticipantsTask.Result;
        var cachedParticipants = cachedParticipantsTask.Result;

        var cachedById = cachedParticipants.ToDictionary(p => p.UserId.Value);
        var liveKitIds = liveKitParticipants.Select(p => p.UserId.Value).ToHashSet();

        var missingIds = liveKitParticipants
            .Where(p => !cachedById.ContainsKey(p.UserId.Value))
            .Select(p => p.UserId)
            .ToArray();

        var fetchedUsers = missingIds.Length > 0
            ? await _userRepository.GetManyByIdsAsync(missingIds, cancellationToken)
            : [];
        var fetchedById = fetchedUsers.ToDictionary(u => u.Id.Value);

        var reconciledParticipants = new List<CachedVoiceParticipant>(liveKitParticipants.Count);

        foreach (var lkParticipant in liveKitParticipants)
        {
            CachedVoiceParticipant cached;

            if (cachedById.TryGetValue(lkParticipant.UserId.Value, out var existing))
            {
                cached = existing;
            }
            else if (fetchedById.TryGetValue(lkParticipant.UserId.Value, out var dbUser))
            {
                cached = new CachedVoiceParticipant(
                    UserId: dbUser.Id,
                    Username: dbUser.Username.Value,
                    DisplayName: dbUser.DisplayName,
                    AvatarFileId: dbUser.AvatarFileId,
                    AvatarColor: dbUser.AvatarColor,
                    AvatarIcon: dbUser.AvatarIcon,
                    AvatarBg: dbUser.AvatarBg);
            }
            else
            {
                cached = new CachedVoiceParticipant(
                    UserId: lkParticipant.UserId,
                    Username: lkParticipant.Username,
                    DisplayName: null,
                    AvatarFileId: null,
                    AvatarColor: null,
                    AvatarIcon: null,
                    AvatarBg: null);
            }

            await _voiceParticipantCache.AddOrUpdateAsync(request, cached, cancellationToken);
            // IsSharingScreen is derived from the SID set in cache; use LiveKit state for the immediate response.
            reconciledParticipants.Add(cached with { IsSharingScreen = lkParticipant.IsSharingScreen });
        }

        foreach (var stale in cachedParticipants.Where(p => !liveKitIds.Contains(p.UserId.Value)))
            await _voiceParticipantCache.RemoveAsync(request, stale.UserId, cancellationToken);

        var currentParticipants = reconciledParticipants
            .Select(p => new JoinConversationVoiceParticipantResponse(
                UserId: p.UserId.Value,
                Username: p.Username,
                DisplayName: p.DisplayName,
                AvatarFileId: p.AvatarFileId?.Value,
                AvatarColor: p.AvatarColor,
                AvatarIcon: p.AvatarIcon,
                AvatarBg: p.AvatarBg,
                IsSharingScreen: p.IsSharingScreen))
            .ToArray();

        var payload = new JoinConversationVoiceResponse(
            Token: roomToken.Token,
            Url: roomToken.Url,
            RoomName: roomToken.RoomName,
            CurrentParticipants: currentParticipants);

        return ApplicationResponse<JoinConversationVoiceResponse>.Ok(payload);
    }
}
