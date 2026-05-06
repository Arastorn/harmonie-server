using Harmonie.Application.Interfaces.Voice;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;
using Harmonie.Infrastructure.Configuration;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.Extensions.Options;

namespace Harmonie.Infrastructure.Authentication;

public sealed class LiveKitTokenService : ILiveKitTokenService
{
    private readonly LiveKitSettings _settings;

    public LiveKitTokenService(IOptions<LiveKitSettings> settings) => _settings = settings.Value;

    public Task<LiveKitRoomToken> GenerateRoomTokenAsync(
        GuildChannelId channelId,
        UserId userId,
        string username,
        CancellationToken ct)
    {
        return Task.FromResult(BuildToken($"channel:{channelId}", userId, username));
    }

    public Task<LiveKitRoomToken> GenerateConversationRoomTokenAsync(
        ConversationId conversationId,
        UserId userId,
        string username,
        CancellationToken ct)
    {
        return Task.FromResult(BuildToken($"conversation:{conversationId}", userId, username));
    }

    private LiveKitRoomToken BuildToken(string roomName, UserId userId, string username)
    {
        var jwt = new AccessToken(_settings.ApiKey, _settings.ApiSecret)
            .WithIdentity(userId.ToString())
            .WithName(username)
            .WithGrants(new VideoGrants { RoomJoin = true, Room = roomName })
            .ToJwt();

        return new LiveKitRoomToken(jwt, _settings.PublicUrl, roomName);
    }
}
