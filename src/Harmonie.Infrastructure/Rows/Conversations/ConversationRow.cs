namespace Harmonie.Infrastructure.Rows;

public sealed class ConversationRow
{
    public Guid Id { get; init; }

    public Guid User1Id { get; init; }

    public Guid User2Id { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
