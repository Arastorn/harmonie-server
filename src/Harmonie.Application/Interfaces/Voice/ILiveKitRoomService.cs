using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Voice;

public interface ILiveKitRoomService
{
    Task<IReadOnlyList<GuildVoiceChannelParticipants>> GetGuildVoiceParticipantsAsync(
        GuildId guildId,
        CancellationToken ct);
}

public sealed record GuildVoiceChannelParticipants(
    GuildChannelId ChannelId,
    IReadOnlyList<VoiceChannelParticipant> Participants);

public sealed record VoiceChannelParticipant(
    UserId UserId,
    string Username);
