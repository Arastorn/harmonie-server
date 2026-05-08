using Harmonie.Domain.Common;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Domain.Entities.Messages;

public sealed class Message : Entity<MessageId>
{
    public GuildChannelId? ChannelId { get; private set; }

    public ConversationId? ConversationId { get; private set; }

    public UserId AuthorUserId { get; private set; }

    public MessageContent? Content { get; private set; }

    public MessageId? ReplyToMessageId { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    private Message(
        MessageId id,
        GuildChannelId? channelId,
        ConversationId? conversationId,
        UserId authorUserId,
        MessageId? replyToMessageId,
        MessageContent? content,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc,
        DateTime? deletedAtUtc)
    {
        Id = id;
        ChannelId = channelId;
        ConversationId = conversationId;
        AuthorUserId = authorUserId;
        ReplyToMessageId = replyToMessageId;
        Content = content;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        DeletedAtUtc = deletedAtUtc;
    }

    public static Result<Message> CreateForChannel(
        GuildChannelId channelId,
        UserId authorUserId,
        MessageContent? content,
        MessageId? replyToMessageId = null)
    {
        if (channelId is null)
            return Result.Failure<Message>("Channel ID is required");
        if (authorUserId is null)
            return Result.Failure<Message>("Author user ID is required");

        return Result.Success(new Message(
            MessageId.New(),
            channelId,
            conversationId: null,
            authorUserId,
            replyToMessageId,
            content,
            DateTime.UtcNow,
            updatedAtUtc: null,
            deletedAtUtc: null));
    }

    public static Result<Message> CreateForConversation(
        ConversationId conversationId,
        UserId authorUserId,
        MessageContent? content,
        MessageId? replyToMessageId = null)
    {
        if (conversationId is null)
            return Result.Failure<Message>("Conversation ID is required");
        if (authorUserId is null)
            return Result.Failure<Message>("Author user ID is required");

        return Result.Success(new Message(
            MessageId.New(),
            channelId: null,
            conversationId,
            authorUserId,
            replyToMessageId,
            content,
            DateTime.UtcNow,
            updatedAtUtc: null,
            deletedAtUtc: null));
    }

    public Result UpdateContent(MessageContent newContent)
    {
        Content = newContent;
        MarkAsUpdated();
        return Result.Success();
    }

    public Result Delete()
    {
        if (DeletedAtUtc is not null)
            return Result.Failure("Message is already deleted");

        DeletedAtUtc = DateTime.UtcNow;
        MarkAsUpdated();
        return Result.Success();
    }

    public static Message Rehydrate(
        MessageId id,
        GuildChannelId? channelId,
        ConversationId? conversationId,
        UserId authorUserId,
        MessageId? replyToMessageId,
        MessageContent? content,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc,
        DateTime? deletedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(authorUserId);

        if ((channelId is null) == (conversationId is null))
            throw new ArgumentException("Exactly one parent reference is required.", nameof(channelId));

        return new Message(
            id,
            channelId,
            conversationId,
            authorUserId,
            replyToMessageId,
            content,
            createdAtUtc,
            updatedAtUtc,
            deletedAtUtc);
    }
}
