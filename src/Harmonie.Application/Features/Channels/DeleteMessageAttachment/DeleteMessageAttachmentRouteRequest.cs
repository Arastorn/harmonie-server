namespace Harmonie.Application.Features.Channels.DeleteMessageAttachment;

public sealed class DeleteMessageAttachmentRouteRequest
{
    public string? ChannelId { get; init; }
    public string? MessageId { get; init; }
    public string? AttachmentId { get; init; }
}
