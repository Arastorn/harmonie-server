namespace Harmonie.Application.Common.Messages;

public sealed record MessageReactionDto(
    string Emoji,
    int Count,
    bool ReactedByMe);
