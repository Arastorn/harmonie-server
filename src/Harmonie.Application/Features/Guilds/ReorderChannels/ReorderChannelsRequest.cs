namespace Harmonie.Application.Features.Guilds.ReorderChannels;

public sealed class ReorderChannelsRouteRequest
{
    public string? GuildId { get; init; }
}

public sealed record ReorderChannelsRequest(
    IReadOnlyList<ReorderChannelsItemRequest> Channels);

public sealed record ReorderChannelsItemRequest(
    string ChannelId,
    int Position);
