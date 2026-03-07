using Harmonie.Infrastructure.Configuration;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.Extensions.Options;

namespace Harmonie.Infrastructure.LiveKit;

public sealed class LiveKitSdkRoomApiClient : ILiveKitRoomApiClient
{
    private readonly LiveKitSettings _settings;
    private readonly string _serverUrl;

    public LiveKitSdkRoomApiClient(IOptions<LiveKitSettings> settings)
    {
        _settings = settings.Value;
        _serverUrl = _settings.GetServerUrl();

        if (string.IsNullOrWhiteSpace(_serverUrl))
            throw new InvalidOperationException("Configuration value 'LiveKit:Url' or 'LiveKit:ServerUrl' is required.");

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Configuration value 'LiveKit:ApiKey' is required.");

        if (string.IsNullOrWhiteSpace(_settings.ApiSecret))
            throw new InvalidOperationException("Configuration value 'LiveKit:ApiSecret' is required.");
    }

    public async Task<IReadOnlyList<Room>> ListRoomsAsync(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        var roomServiceClient = new RoomServiceClient(
            _serverUrl,
            _settings.ApiKey,
            _settings.ApiSecret,
            httpClient);

        var response = await roomServiceClient.ListRooms(new ListRoomsRequest());
        return response.Rooms.ToArray();
    }

    public async Task<IReadOnlyList<ParticipantInfo>> ListParticipantsAsync(
        string roomName,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        var roomServiceClient = new RoomServiceClient(
            _serverUrl,
            _settings.ApiKey,
            _settings.ApiSecret,
            httpClient);

        var response = await roomServiceClient.ListParticipants(new ListParticipantsRequest
        {
            Room = roomName
        });

        return response.Participants.ToArray();
    }
}
