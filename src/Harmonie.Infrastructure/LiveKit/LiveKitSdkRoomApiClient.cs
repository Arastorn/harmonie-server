using Harmonie.Infrastructure.Configuration;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.Extensions.Options;

namespace Harmonie.Infrastructure.LiveKit;

public sealed class LiveKitSdkRoomApiClient : ILiveKitRoomApiClient
{
    private readonly RoomServiceClient _roomServiceClient;

    public LiveKitSdkRoomApiClient(HttpClient httpClient, IOptions<LiveKitSettings> settings)
    {
        var liveKitSettings = settings.Value;
        var internalUrl = liveKitSettings.GetInternalUrl();

        if (string.IsNullOrWhiteSpace(internalUrl))
            throw new InvalidOperationException("Configuration value 'LiveKit:PublicUrl' or 'LiveKit:InternalUrl' is required.");

        if (string.IsNullOrWhiteSpace(liveKitSettings.ApiKey))
            throw new InvalidOperationException("Configuration value 'LiveKit:ApiKey' is required.");

        if (string.IsNullOrWhiteSpace(liveKitSettings.ApiSecret))
            throw new InvalidOperationException("Configuration value 'LiveKit:ApiSecret' is required.");

        httpClient.Timeout = TimeSpan.FromSeconds(liveKitSettings.RequestTimeoutSeconds);

        _roomServiceClient = new RoomServiceClient(
            internalUrl,
            liveKitSettings.ApiKey,
            liveKitSettings.ApiSecret,
            httpClient);
    }

    public async Task<IReadOnlyList<Room>> ListRoomsAsync(CancellationToken cancellationToken)
    {
        var response = await _roomServiceClient
            .ListRooms(new ListRoomsRequest())
            .WaitAsync(cancellationToken);
        return response.Rooms.ToArray();
    }

    public async Task<IReadOnlyList<ParticipantInfo>> ListParticipantsAsync(
        string roomName,
        CancellationToken cancellationToken)
    {
        var response = await _roomServiceClient
            .ListParticipants(new ListParticipantsRequest
            {
                Room = roomName
            })
            .WaitAsync(cancellationToken);

        return response.Participants.ToArray();
    }
}
