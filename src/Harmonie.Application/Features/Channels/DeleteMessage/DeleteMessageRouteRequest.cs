namespace Harmonie.Application.Features.Channels.DeleteMessage;

public sealed class DeleteMessageRouteRequest
{
    public string? ChannelId { get; init; }
    public string? MessageId { get; init; }
}
