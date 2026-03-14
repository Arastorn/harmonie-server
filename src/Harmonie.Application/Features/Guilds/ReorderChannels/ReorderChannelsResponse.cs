namespace Harmonie.Application.Features.Guilds.ReorderChannels;

public sealed record ReorderChannelsResponse(
    string GuildId,
    IReadOnlyList<ReorderChannelsItemResponse> Channels);

public sealed record ReorderChannelsItemResponse(
    string ChannelId,
    string Name,
    string Type,
    bool IsDefault,
    int Position);
