namespace Harmonie.Domain.ValueObjects.Conversations;

public sealed record ConversationId
{
    public Guid Value { get; }

    private ConversationId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Conversation ID cannot be empty", nameof(value));

        Value = value;
    }

    public static ConversationId New() => new(Guid.NewGuid());

    public static ConversationId From(Guid value) => new(value);

    public static bool TryParse(string? value, out ConversationId? conversationId)
    {
        conversationId = null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!Guid.TryParse(value, out var parsed) || parsed == Guid.Empty)
            return false;

        conversationId = new ConversationId(parsed);
        return true;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(ConversationId conversationId) => conversationId.Value;
}
