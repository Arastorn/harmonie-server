using Harmonie.Application.Common.Messages;

namespace Harmonie.API.RealTime.Messages;

public sealed record MessageCreatedEvent(
    Guid MessageId,
    Guid ChannelId,
    string ChannelName,
    Guid GuildId,
    string GuildName,
    Guid AuthorUserId,
    string AuthorUsername,
    string? AuthorDisplayName,
    string? Content,
    IReadOnlyList<MessageAttachmentDto> Attachments,
    ReplyPreviewDto? ReplyTo,
    IReadOnlyList<Guid> MentionedUserIds,
    DateTime CreatedAtUtc);

public sealed record MessageUpdatedEvent(
    Guid MessageId,
    Guid ChannelId,
    string ChannelName,
    Guid GuildId,
    string GuildName,
    string? Content,
    IReadOnlyList<Guid> MentionedUserIds,
    DateTime UpdatedAtUtc);

public sealed record MessageDeletedEvent(
    Guid MessageId,
    Guid ChannelId,
    string ChannelName,
    Guid GuildId,
    string GuildName);

public sealed record MessagePreviewUpdatedEvent(
    Guid MessageId,
    Guid? ChannelId,
    string? ChannelName,
    Guid? ConversationId,
    string? ConversationName,
    string? ConversationType,
    Guid? GuildId,
    string? GuildName,
    IReadOnlyList<LinkPreviewDto> Previews);

public sealed record ConversationMessageCreatedEvent(
    Guid MessageId,
    Guid ConversationId,
    string? ConversationName,
    string ConversationType,
    Guid AuthorUserId,
    string AuthorUsername,
    string? AuthorDisplayName,
    string? Content,
    IReadOnlyList<MessageAttachmentDto> Attachments,
    ReplyPreviewDto? ReplyTo,
    IReadOnlyList<Guid> MentionedUserIds,
    DateTime CreatedAtUtc);

public sealed record ConversationMessageUpdatedEvent(
    Guid MessageId,
    Guid ConversationId,
    string? ConversationName,
    string ConversationType,
    string? Content,
    IReadOnlyList<Guid> MentionedUserIds,
    DateTime UpdatedAtUtc);

public sealed record ConversationMessageDeletedEvent(
    Guid MessageId,
    Guid ConversationId,
    string? ConversationName,
    string ConversationType);
