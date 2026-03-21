using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Voice;

public sealed record LiveKitRoomToken(
    string Token,
    string Url,
    string RoomName);

public interface ILiveKitTokenService
{
    Task<LiveKitRoomToken> GenerateRoomTokenAsync(
        GuildChannelId channelId,
        UserId userId,
        string username,
        CancellationToken ct);
}
