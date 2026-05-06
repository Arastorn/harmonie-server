using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Voice;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Voice.HandleLiveKitWebhook;

public sealed class HandleLiveKitWebhookHandler : IHandler<HandleLiveKitWebhookRequest, HandleLiveKitWebhookResponse>
{
    private const string ParticipantJoinedEvent = "participant_joined";
    private const string ParticipantLeftEvent = "participant_left";
    private const string TrackPublishedEvent = "track_published";
    private const string TrackUnpublishedEvent = "track_unpublished";
    private const string ChannelRoomPrefix = "channel:";
    private const string ConversationRoomPrefix = "conversation:";
    private const string ScreenShareSource = "SCREEN_SHARE";

    private readonly ILiveKitWebhookReceiver _webhookReceiver;
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IVoicePresenceNotifier _voicePresenceNotifier;
    private readonly IVoiceParticipantCache _voiceParticipantCache;
    private readonly IConversationVoicePresenceNotifier _conversationVoicePresenceNotifier;
    private readonly IConversationVoiceParticipantCache _conversationVoiceParticipantCache;

    public HandleLiveKitWebhookHandler(
        ILiveKitWebhookReceiver webhookReceiver,
        IGuildChannelRepository guildChannelRepository,
        IConversationRepository conversationRepository,
        IVoicePresenceNotifier voicePresenceNotifier,
        IVoiceParticipantCache voiceParticipantCache,
        IConversationVoicePresenceNotifier conversationVoicePresenceNotifier,
        IConversationVoiceParticipantCache conversationVoiceParticipantCache)
    {
        _webhookReceiver = webhookReceiver;
        _guildChannelRepository = guildChannelRepository;
        _conversationRepository = conversationRepository;
        _voicePresenceNotifier = voicePresenceNotifier;
        _voiceParticipantCache = voiceParticipantCache;
        _conversationVoicePresenceNotifier = conversationVoicePresenceNotifier;
        _conversationVoiceParticipantCache = conversationVoiceParticipantCache;
    }

    public async Task<ApplicationResponse<HandleLiveKitWebhookResponse>> HandleAsync(
        HandleLiveKitWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        var receiveResult = _webhookReceiver.Receive(request.RawBody, request.AuthorizationHeader ?? string.Empty);
        if (!receiveResult.Success)
        {
            return ApplicationResponse<HandleLiveKitWebhookResponse>.Fail(
                ApplicationErrorCodes.Auth.InvalidCredentials,
                "LiveKit webhook signature is invalid.");
        }

        if (receiveResult.Event is null)
        {
            return ApplicationResponse<HandleLiveKitWebhookResponse>.Fail(
                ApplicationErrorCodes.Common.InvalidState,
                "LiveKit webhook receiver returned success without an event.");
        }

        var webhookEvent = receiveResult.Event;
        var eventType = string.IsNullOrWhiteSpace(webhookEvent.EventType)
            ? "unknown"
            : webhookEvent.EventType;

        if (eventType is not ParticipantJoinedEvent
            and not ParticipantLeftEvent
            and not TrackPublishedEvent
            and not TrackUnpublishedEvent)
        {
            return ApplicationResponse<HandleLiveKitWebhookResponse>.Ok(new(false, eventType));
        }

        if (!TryParseUserId(webhookEvent.ParticipantIdentity, out var participantUserId) || participantUserId is null)
        {
            return ApplicationResponse<HandleLiveKitWebhookResponse>.Ok(new(false, eventType));
        }

        if (TryParseChannelId(webhookEvent.RoomName, out var channelId) && channelId is not null)
        {
            var result = await _guildChannelRepository.GetWithParticipantAsync(channelId, participantUserId, cancellationToken);
            if (result is null || result.Channel.Type != GuildChannelType.Voice)
                return ApplicationResponse<HandleLiveKitWebhookResponse>.Ok(new(false, eventType));

            if (eventType == ParticipantJoinedEvent)
                await HandleParticipantJoinedAsync(result, participantUserId, webhookEvent.OccurredAtUtc, cancellationToken);
            else if (eventType == ParticipantLeftEvent)
                await HandleParticipantLeftAsync(result, participantUserId, webhookEvent.OccurredAtUtc, cancellationToken);
            else if (eventType is TrackPublishedEvent or TrackUnpublishedEvent)
                await HandleTrackEventAsync(eventType, result, participantUserId, webhookEvent, cancellationToken);

            return ApplicationResponse<HandleLiveKitWebhookResponse>.Ok(new(true, eventType));
        }

        if (TryParseConversationId(webhookEvent.RoomName, out var conversationId) && conversationId is not null)
        {
            var result = await _conversationRepository.GetWithParticipantAsync(conversationId, participantUserId, cancellationToken);
            if (result is null)
                return ApplicationResponse<HandleLiveKitWebhookResponse>.Ok(new(false, eventType));

            if (eventType == ParticipantJoinedEvent)
                await HandleConversationParticipantJoinedAsync(result, participantUserId, webhookEvent.OccurredAtUtc, cancellationToken);
            else if (eventType == ParticipantLeftEvent)
                await HandleConversationParticipantLeftAsync(result, participantUserId, webhookEvent.OccurredAtUtc, cancellationToken);
            else if (eventType is TrackPublishedEvent or TrackUnpublishedEvent)
                await HandleConversationTrackEventAsync(eventType, result, participantUserId, webhookEvent, cancellationToken);

            return ApplicationResponse<HandleLiveKitWebhookResponse>.Ok(new(true, eventType));
        }

        return ApplicationResponse<HandleLiveKitWebhookResponse>.Ok(new(false, eventType));
    }

    private static bool TryParseChannelId(string? roomName, out GuildChannelId? channelId)
    {
        channelId = null;

        if (string.IsNullOrWhiteSpace(roomName)
            || !roomName.StartsWith(ChannelRoomPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rawChannelId = roomName[ChannelRoomPrefix.Length..];
        if (!GuildChannelId.TryParse(rawChannelId, out channelId) || channelId is null)
            return false;

        return true;
    }

    private async Task HandleParticipantJoinedAsync(
        ChannelWithParticipant result,
        UserId participantUserId,
        DateTime occurredAtUtc,
        CancellationToken ct)
    {
        await _voiceParticipantCache.AddOrUpdateAsync(
            result.Channel.Id,
            new CachedVoiceParticipant(
                UserId: participantUserId,
                Username: result.Participant?.Username.Value,
                DisplayName: result.Participant?.DisplayName,
                AvatarFileId: result.Participant?.AvatarFileId,
                AvatarColor: result.Participant?.AvatarColor,
                AvatarIcon: result.Participant?.AvatarIcon,
                AvatarBg: result.Participant?.AvatarBg),
            ct);

        await _voicePresenceNotifier.NotifyParticipantJoinedAsync(
            new VoiceParticipantJoinedNotification(
                GuildId: result.Channel.GuildId,
                GuildName: result.GuildName,
                ChannelId: result.Channel.Id,
                ChannelName: result.Channel.Name,
                UserId: participantUserId,
                Username: result.Participant?.Username.Value,
                DisplayName: result.Participant?.DisplayName,
                AvatarFileId: result.Participant?.AvatarFileId,
                AvatarColor: result.Participant?.AvatarColor,
                AvatarIcon: result.Participant?.AvatarIcon,
                AvatarBg: result.Participant?.AvatarBg,
                JoinedAtUtc: occurredAtUtc),
            ct);
    }

    private async Task HandleParticipantLeftAsync(
        ChannelWithParticipant result,
        UserId participantUserId,
        DateTime occurredAtUtc,
        CancellationToken ct)
    {
        // Clear screen share tracks for safety (LiveKit sends track_unpublished before participant_left,
        // but we clean up here as a safety net).
        await _voiceParticipantCache.ClearScreenShareTracksAsync(
            result.Channel.Id, participantUserId, ct);

        await _voiceParticipantCache.RemoveAsync(result.Channel.Id, participantUserId, ct);

        await _voicePresenceNotifier.NotifyParticipantLeftAsync(
            new VoiceParticipantLeftNotification(
                GuildId: result.Channel.GuildId,
                GuildName: result.GuildName,
                ChannelId: result.Channel.Id,
                ChannelName: result.Channel.Name,
                UserId: participantUserId,
                Username: result.Participant?.Username.Value,
                LeftAtUtc: occurredAtUtc),
            ct);
    }

    private async Task HandleTrackEventAsync(
        string eventType,
        ChannelWithParticipant result,
        UserId participantUserId,
        LiveKitWebhookEvent webhookEvent,
        CancellationToken ct)
    {
        if (webhookEvent.Track is null)
            return;

        var track = webhookEvent.Track;

        // Only process ScreenShare tracks. Ignore SCREEN_SHARE_AUDIO, CAMERA, MICROPHONE.
        if (track.Source != ScreenShareSource)
            return;

        var guildId = result.Channel.GuildId;
        var guildName = result.GuildName;
        var channelId = result.Channel.Id;
        var channelName = result.Channel.Name;
        var username = result.Participant?.Username.Value;

        if (eventType == TrackPublishedEvent)
        {
            var addResult = await _voiceParticipantCache.TryAddScreenShareTrackAsync(
                channelId, participantUserId, track.Sid, ct);

            if (addResult.IsFirst)
            {
                await _voicePresenceNotifier.NotifyScreenShareStartedAsync(
                    new VoiceScreenShareNotification(
                        guildId, guildName, channelId, channelName, participantUserId, username, webhookEvent.OccurredAtUtc),
                    ct);
            }
        }
        else if (eventType == TrackUnpublishedEvent)
        {
            var removeResult = await _voiceParticipantCache.TryRemoveScreenShareTrackAsync(
                channelId, participantUserId, track.Sid, ct);

            if (removeResult.IsLast)
            {
                await _voicePresenceNotifier.NotifyScreenShareStoppedAsync(
                    new VoiceScreenShareNotification(
                        guildId, guildName, channelId, channelName, participantUserId, username, webhookEvent.OccurredAtUtc),
                    ct);
            }
        }
    }

    private static bool TryParseConversationId(string? roomName, out ConversationId? conversationId)
    {
        conversationId = null;

        if (string.IsNullOrWhiteSpace(roomName)
            || !roomName.StartsWith(ConversationRoomPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rawConversationId = roomName[ConversationRoomPrefix.Length..];
        if (!ConversationId.TryParse(rawConversationId, out conversationId) || conversationId is null)
            return false;

        return true;
    }

    private static bool TryParseUserId(string? participantIdentity, out UserId? userId)
    {
        userId = null;

        if (string.IsNullOrWhiteSpace(participantIdentity))
            return false;

        if (!UserId.TryParse(participantIdentity, out userId) || userId is null)
            return false;

        return true;
    }

    private async Task HandleConversationParticipantJoinedAsync(
        ConversationWithParticipant result,
        UserId participantUserId,
        DateTime occurredAtUtc,
        CancellationToken ct)
    {
        await _conversationVoiceParticipantCache.AddOrUpdateAsync(
            result.Conversation.Id,
            new CachedVoiceParticipant(
                UserId: participantUserId,
                Username: result.Participant?.Username.Value,
                DisplayName: result.Participant?.DisplayName,
                AvatarFileId: result.Participant?.AvatarFileId,
                AvatarColor: result.Participant?.AvatarColor,
                AvatarIcon: result.Participant?.AvatarIcon,
                AvatarBg: result.Participant?.AvatarBg),
            ct);

        await _conversationVoicePresenceNotifier.NotifyParticipantJoinedAsync(
            new ConversationVoiceParticipantJoinedNotification(
                ConversationId: result.Conversation.Id,
                UserId: participantUserId,
                Username: result.Participant?.Username.Value,
                DisplayName: result.Participant?.DisplayName,
                AvatarFileId: result.Participant?.AvatarFileId,
                AvatarColor: result.Participant?.AvatarColor,
                AvatarIcon: result.Participant?.AvatarIcon,
                AvatarBg: result.Participant?.AvatarBg,
                JoinedAtUtc: occurredAtUtc),
            ct);
    }

    private async Task HandleConversationParticipantLeftAsync(
        ConversationWithParticipant result,
        UserId participantUserId,
        DateTime occurredAtUtc,
        CancellationToken ct)
    {
        // Clear screen share tracks for safety (LiveKit sends track_unpublished before participant_left,
        // but we clean up here as a safety net).
        await _conversationVoiceParticipantCache.ClearScreenShareTracksAsync(
            result.Conversation.Id, participantUserId, ct);

        await _conversationVoiceParticipantCache.RemoveAsync(result.Conversation.Id, participantUserId, ct);

        await _conversationVoicePresenceNotifier.NotifyParticipantLeftAsync(
            new ConversationVoiceParticipantLeftNotification(
                ConversationId: result.Conversation.Id,
                UserId: participantUserId,
                Username: result.Participant?.Username.Value,
                LeftAtUtc: occurredAtUtc),
            ct);
    }

    private async Task HandleConversationTrackEventAsync(
        string eventType,
        ConversationWithParticipant result,
        UserId participantUserId,
        LiveKitWebhookEvent webhookEvent,
        CancellationToken ct)
    {
        if (webhookEvent.Track is null)
            return;

        var track = webhookEvent.Track;

        // Only process ScreenShare tracks. Ignore SCREEN_SHARE_AUDIO, CAMERA, MICROPHONE.
        if (track.Source != ScreenShareSource)
            return;

        var conversationId = result.Conversation.Id;
        var username = result.Participant?.Username.Value;

        if (eventType == TrackPublishedEvent)
        {
            var addResult = await _conversationVoiceParticipantCache.TryAddScreenShareTrackAsync(
                conversationId, participantUserId, track.Sid, ct);

            if (addResult.IsFirst)
            {
                await _conversationVoicePresenceNotifier.NotifyScreenShareStartedAsync(
                    new ConversationVoiceScreenShareNotification(
                        conversationId, participantUserId, username, webhookEvent.OccurredAtUtc),
                    ct);
            }
        }
        else if (eventType == TrackUnpublishedEvent)
        {
            var removeResult = await _conversationVoiceParticipantCache.TryRemoveScreenShareTrackAsync(
                conversationId, participantUserId, track.Sid, ct);

            if (removeResult.IsLast)
            {
                await _conversationVoicePresenceNotifier.NotifyScreenShareStoppedAsync(
                    new ConversationVoiceScreenShareNotification(
                        conversationId, participantUserId, username, webhookEvent.OccurredAtUtc),
                    ct);
            }
        }
    }
}
