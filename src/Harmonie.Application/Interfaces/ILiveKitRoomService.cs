using Harmonie.Domain.ValueObjects;

namespace Harmonie.Application.Interfaces;

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
