namespace Harmonie.Application.Features.Conversations.SendMessage;

public sealed record SendMessageRequest(
    string Content,
    IReadOnlyList<string>? AttachmentFileIds = null);
