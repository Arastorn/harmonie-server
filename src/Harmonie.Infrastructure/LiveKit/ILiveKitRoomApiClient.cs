using Livekit.Server.Sdk.Dotnet;

namespace Harmonie.Infrastructure.LiveKit;

public interface ILiveKitRoomApiClient
{
    Task<IReadOnlyList<Room>> ListRoomsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ParticipantInfo>> ListParticipantsAsync(
        string roomName,
        CancellationToken cancellationToken);
}
