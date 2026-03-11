namespace Harmonie.Application.Features.Conversations.GetMessages;

public sealed class GetMessagesRequest
{
    public string? Cursor { get; init; }

    public int? Limit { get; init; }
}
