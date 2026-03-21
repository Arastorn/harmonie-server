namespace Harmonie.Application.Common;

public sealed record MessageReactionDto(
    string Emoji,
    int Count,
    bool ReactedByMe);
