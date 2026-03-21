using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Voice;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Infrastructure.LiveKit;

public sealed class LiveKitRoomService : ILiveKitRoomService
{
    private const string ChannelRoomPrefix = "channel:";

    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly ILiveKitRoomApiClient _roomApiClient;
    private readonly ILogger<LiveKitRoomService> _logger;

    public LiveKitRoomService(
        IGuildChannelRepository guildChannelRepository,
        ILiveKitRoomApiClient roomApiClient,
        ILogger<LiveKitRoomService> logger)
    {
        _guildChannelRepository = guildChannelRepository;
        _roomApiClient = roomApiClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GuildVoiceChannelParticipants>> GetGuildVoiceParticipantsAsync(
        GuildId guildId,
        CancellationToken ct)
    {
        var guildChannels = await _guildChannelRepository.GetByGuildIdAsync(guildId, ct);
        var voiceChannels = guildChannels
            .Where(channel => channel.Type == GuildChannelType.Voice)
            .ToArray();

        if (voiceChannels.Length == 0)
            return [];

        var activeRooms = await _roomApiClient.ListRoomsAsync(ct);
        var activeRoomNames = activeRooms
            .Select(room => room.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.Ordinal);

        var channels = new List<GuildVoiceChannelParticipants>();

        foreach (var voiceChannel in voiceChannels)
        {
            var roomName = BuildRoomName(voiceChannel.Id);
            if (!activeRoomNames.Contains(roomName))
            {
                _logger.LogDebug(
                    "LiveKit room is inactive and will be skipped. GuildId={GuildId}, ChannelId={ChannelId}, RoomName={RoomName}",
                    guildId,
                    voiceChannel.Id,
                    roomName);
                continue;
            }

            var participants = await _roomApiClient.ListParticipantsAsync(roomName, ct);
            if (participants.Count == 0)
            {
                _logger.LogDebug(
                    "LiveKit room has no active participants and will be omitted. GuildId={GuildId}, ChannelId={ChannelId}, RoomName={RoomName}",
                    guildId,
                    voiceChannel.Id,
                    roomName);
                continue;
            }

            var mappedParticipants = participants
                .Select(participant => TryMapParticipant(participant.Identity, participant.Name))
                .OfType<VoiceChannelParticipant>()
                .ToArray();

            if (mappedParticipants.Length == 0)
            {
                _logger.LogDebug(
                    "LiveKit room contained no valid application participants. GuildId={GuildId}, ChannelId={ChannelId}, RoomName={RoomName}",
                    guildId,
                    voiceChannel.Id,
                    roomName);
                continue;
            }

            channels.Add(new GuildVoiceChannelParticipants(voiceChannel.Id, mappedParticipants));
        }

        return channels;
    }

    private VoiceChannelParticipant? TryMapParticipant(string? identity, string? name)
    {
        if (string.IsNullOrWhiteSpace(identity)
            || !UserId.TryParse(identity, out var userId)
            || userId is null)
        {
            _logger.LogWarning(
                "LiveKit participant identity could not be mapped to a user. Identity={Identity}",
                identity);
            return null;
        }

        var username = string.IsNullOrWhiteSpace(name) ? identity : name;
        return new VoiceChannelParticipant(userId, username);
    }

    private static string BuildRoomName(GuildChannelId channelId)
        => $"{ChannelRoomPrefix}{channelId}";
}
