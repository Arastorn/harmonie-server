namespace Harmonie.Domain.ValueObjects;

public sealed record DirectMessageId
{
    public Guid Value { get; }

    private DirectMessageId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Direct message ID cannot be empty", nameof(value));

        Value = value;
    }

    public static DirectMessageId New() => new(Guid.NewGuid());

    public static DirectMessageId From(Guid value) => new(value);

    public static bool TryParse(string? value, out DirectMessageId? directMessageId)
    {
        directMessageId = null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!Guid.TryParse(value, out var parsed) || parsed == Guid.Empty)
            return false;

        directMessageId = new DirectMessageId(parsed);
        return true;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(DirectMessageId directMessageId) => directMessageId.Value;
}
