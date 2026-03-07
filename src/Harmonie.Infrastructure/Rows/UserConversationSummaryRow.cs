namespace Harmonie.Infrastructure.Rows;

public sealed class UserConversationSummaryRow
{
    public Guid ConversationId { get; init; }

    public Guid OtherParticipantUserId { get; init; }

    public string OtherParticipantUsername { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}
