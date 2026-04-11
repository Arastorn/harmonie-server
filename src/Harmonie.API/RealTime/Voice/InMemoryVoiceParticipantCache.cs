using System.Collections.Concurrent;
using Harmonie.Application.Interfaces.Voice;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.API.RealTime.Voice;

public sealed class InMemoryVoiceParticipantCache : IVoiceParticipantCache
{
    private static readonly TimeSpan ParticipantTtl = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, CachedEntry>> _channels = new();

    public Task AddOrUpdateAsync(GuildChannelId channelId, CachedVoiceParticipant participant, CancellationToken cancellationToken = default)
    {
        var channel = _channels.GetOrAdd(channelId.Value, _ => new ConcurrentDictionary<Guid, CachedEntry>());
        channel[participant.UserId.Value] = new CachedEntry(participant, DateTime.UtcNow + ParticipantTtl);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(GuildChannelId channelId, UserId userId, CancellationToken cancellationToken = default)
    {
        if (_channels.TryGetValue(channelId.Value, out var channel))
            channel.TryRemove(userId.Value, out _);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CachedVoiceParticipant>> GetAsync(GuildChannelId channelId, CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(channelId.Value, out var channel))
            return Task.FromResult<IReadOnlyList<CachedVoiceParticipant>>(Array.Empty<CachedVoiceParticipant>());

        var now = DateTime.UtcNow;
        var result = channel.Values
            .Where(e => e.ExpiresAt > now)
            .Select(e => e.Participant)
            .ToArray();

        return Task.FromResult<IReadOnlyList<CachedVoiceParticipant>>(result);
    }

    private sealed record CachedEntry(CachedVoiceParticipant Participant, DateTime ExpiresAt);
}
