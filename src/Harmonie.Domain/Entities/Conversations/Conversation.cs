using Harmonie.Domain.Common;
using Harmonie.Domain.ValueObjects;

namespace Harmonie.Domain.Entities;

public sealed class Conversation : Entity<ConversationId>
{
    public UserId User1Id { get; private set; }

    public UserId User2Id { get; private set; }

    private Conversation(
        ConversationId id,
        UserId user1Id,
        UserId user2Id,
        DateTime createdAtUtc)
    {
        Id = id;
        User1Id = user1Id;
        User2Id = user2Id;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = null;
    }

    public static Result<Conversation> Create(UserId firstUserId, UserId secondUserId)
    {
        if (firstUserId is null)
            return Result.Failure<Conversation>("First user ID is required");

        if (secondUserId is null)
            return Result.Failure<Conversation>("Second user ID is required");

        if (firstUserId == secondUserId)
            return Result.Failure<Conversation>("Conversation participants must be different users");

        var (user1Id, user2Id) = NormalizeParticipants(firstUserId, secondUserId);

        return Result.Success(new Conversation(
            ConversationId.New(),
            user1Id,
            user2Id,
            DateTime.UtcNow));
    }

    public static Conversation Rehydrate(
        ConversationId id,
        UserId user1Id,
        UserId user2Id,
        DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(user1Id);
        ArgumentNullException.ThrowIfNull(user2Id);

        if (user1Id == user2Id)
            throw new ArgumentException("Conversation participants must be different users.");

        return new Conversation(
            id,
            user1Id,
            user2Id,
            createdAtUtc);
    }

    private static (UserId User1Id, UserId User2Id) NormalizeParticipants(UserId firstUserId, UserId secondUserId)
        => firstUserId.Value.CompareTo(secondUserId.Value) <= 0
            ? (firstUserId, secondUserId)
            : (secondUserId, firstUserId);
}
