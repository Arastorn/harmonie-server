namespace Harmonie.Application.Features.Channels.SendMessage;

public sealed record SendMessageRequest(
    string Content,
    IReadOnlyList<string>? AttachmentFileIds = null);
