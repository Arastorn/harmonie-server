using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Harmonie.Infrastructure.Rows.Messages;

namespace Harmonie.Infrastructure.Persistence.Messages;

internal static class MessageRepositoryHelpers
{
    internal static IReadOnlyDictionary<Guid, IReadOnlyList<MessageReactionSummary>> BuildReactionsDictionary(
        IEnumerable<ReactionSummaryRow> summaryRows,
        IEnumerable<ReactionUserRow> userRows)
    {
        var usersByKey = userRows
            .GroupBy(row => (row.MessageId, row.Emoji))
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ReactionUser>)group
                    .Select(row => new ReactionUser(
                        row.UserId,
                        row.Username,
                        row.DisplayName))
                    .ToArray());

        return summaryRows
            .GroupBy(row => row.MessageId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MessageReactionSummary>)group
                    .Select(row =>
                    {
                        usersByKey.TryGetValue((row.MessageId, row.Emoji), out var users);
                        return new MessageReactionSummary(
                            row.Emoji,
                            row.Count,
                            row.ReactedByCaller,
                            users ?? Array.Empty<ReactionUser>());
                    })
                    .ToArray());
    }

    internal static Message MapToMessage(MessageRow row)
    {
        MessageContent? messageContent = null;
        if (row.Content is not null)
        {
            var contentResult = MessageContent.Create(row.Content);
            if (contentResult.IsFailure || contentResult.Value is null)
                throw new InvalidOperationException("Stored message content is invalid.");
            messageContent = contentResult.Value;
        }

        GuildChannelId? channelId = row.ChannelId.HasValue
            ? GuildChannelId.From(row.ChannelId.Value)
            : null;
        ConversationId? conversationId = row.ConversationId.HasValue
            ? ConversationId.From(row.ConversationId.Value)
            : null;
        MessageId? replyToMessageId = row.ReplyToMessageId.HasValue
            ? MessageId.From(row.ReplyToMessageId.Value)
            : null;

        return Message.Rehydrate(
            MessageId.From(row.Id),
            channelId,
            conversationId,
            UserId.From(row.AuthorUserId),
            replyToMessageId,
            messageContent,
            row.CreatedAtUtc,
            row.UpdatedAtUtc,
            row.DeletedAtUtc);
    }

    internal static IReadOnlyDictionary<Guid, IReadOnlyList<LinkPreviewDto>> BuildLinkPreviewsDictionary(
        IEnumerable<MessageLinkPreviewRow> rows)
    {
        return rows
            .GroupBy(row => row.MessageId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<LinkPreviewDto>)group
                    .Select(row => new LinkPreviewDto(
                        row.Url,
                        row.Title,
                        row.Description,
                        row.ImageUrl,
                        row.SiteName))
                    .ToArray());
    }
}
