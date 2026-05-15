using Harmonie.Application.Common.Messages;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Messages;

public interface IMessageEventPublisher
{
    Task PublishCreatedAsync(MessageCreatedEventEnvelope messageEvent, CancellationToken cancellationToken = default);

    Task PublishUpdatedAsync(MessageUpdatedEventEnvelope messageEvent, CancellationToken cancellationToken = default);

    Task PublishDeletedAsync(MessageDeletedEventEnvelope messageEvent, CancellationToken cancellationToken = default);

    Task PublishPreviewUpdatedAsync(MessagePreviewUpdatedEventEnvelope messageEvent, CancellationToken cancellationToken = default);
}
public abstract record MessageEventLocation
{
    private MessageEventLocation() { }

    public sealed record Channel(
        GuildChannelId ChannelId,
        string ChannelName,
        GuildId GuildId,
        string GuildName) : MessageEventLocation;

    public sealed record Conversation(
        ConversationId ConversationId,
        string? ConversationName,
        string ConversationType) : MessageEventLocation;
}

public sealed record MessageCreatedEventEnvelope(
    MessageId MessageId,
    MessageEventLocation Location,
    UserId AuthorUserId,
    string AuthorUsername,
    string? AuthorDisplayName,
    string? Content,
    IReadOnlyList<MessageAttachmentDto> Attachments,
    ReplyPreviewDto? ReplyTo,
    IReadOnlyList<Guid> MentionedUserIds,
    DateTime CreatedAtUtc)
{
    public GuildChannelId? ChannelId => (Location as MessageEventLocation.Channel)?.ChannelId;
    public GuildId? GuildId => (Location as MessageEventLocation.Channel)?.GuildId;
    public ConversationId? ConversationId => (Location as MessageEventLocation.Conversation)?.ConversationId;
    public string? ConversationName => (Location as MessageEventLocation.Conversation)?.ConversationName;
    public string? ConversationType => (Location as MessageEventLocation.Conversation)?.ConversationType;
}

public sealed record MessageUpdatedEventEnvelope(
    MessageId MessageId,
    MessageEventLocation Location,
    string? Content,
    IReadOnlyList<Guid> MentionedUserIds,
    DateTime UpdatedAtUtc)
{
    public GuildChannelId? ChannelId => (Location as MessageEventLocation.Channel)?.ChannelId;
    public GuildId? GuildId => (Location as MessageEventLocation.Channel)?.GuildId;
    public ConversationId? ConversationId => (Location as MessageEventLocation.Conversation)?.ConversationId;
    public string? ConversationName => (Location as MessageEventLocation.Conversation)?.ConversationName;
    public string? ConversationType => (Location as MessageEventLocation.Conversation)?.ConversationType;
}

public sealed record MessageDeletedEventEnvelope(
    MessageId MessageId,
    MessageEventLocation Location)
{
    public GuildChannelId? ChannelId => (Location as MessageEventLocation.Channel)?.ChannelId;
    public GuildId? GuildId => (Location as MessageEventLocation.Channel)?.GuildId;
    public ConversationId? ConversationId => (Location as MessageEventLocation.Conversation)?.ConversationId;
    public string? ConversationName => (Location as MessageEventLocation.Conversation)?.ConversationName;
    public string? ConversationType => (Location as MessageEventLocation.Conversation)?.ConversationType;
}

public sealed record MessagePreviewUpdatedEventEnvelope(
    MessageId MessageId,
    MessageEventLocation Location,
    IReadOnlyList<LinkPreviewDto> Previews);
