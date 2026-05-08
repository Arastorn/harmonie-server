using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Channels.GetPinnedMessages;

public sealed record GetChannelPinnedMessagesInput(GuildChannelId ChannelId, string? Before = null, int? Limit = null);

public sealed class GetPinnedMessagesHandler : IAuthenticatedHandler<GetChannelPinnedMessagesInput, GetPinnedMessagesResponse>
{
    private const int DefaultLimit = 50;

    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IPinnedMessageRepository _pinnedMessageRepository;
    private readonly IMessageAttachmentRepository _messageAttachmentRepository;

    public GetPinnedMessagesHandler(
        IGuildChannelRepository guildChannelRepository,
        IPinnedMessageRepository pinnedMessageRepository,
        IMessageAttachmentRepository messageAttachmentRepository)
    {
        _guildChannelRepository = guildChannelRepository;
        _pinnedMessageRepository = pinnedMessageRepository;
        _messageAttachmentRepository = messageAttachmentRepository;
    }

    public async Task<ApplicationResponse<GetPinnedMessagesResponse>> HandleAsync(
        GetChannelPinnedMessagesInput request,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        PinnedMessagesCursor? cursor = null;
        if (request.Before is not null)
        {
            if (!PinnedMessagesCursorCodec.TryParse(request.Before, out var parsed) || parsed is null)
            {
                return ApplicationResponse<GetPinnedMessagesResponse>.Fail(
                    ApplicationErrorCodes.Common.ValidationFailed,
                    "Request validation failed",
                    EndpointExtensions.SingleValidationError(
                        nameof(request.Before),
                        ApplicationErrorCodes.Validation.InvalidFormat,
                        "Before cursor is invalid"));
            }

            cursor = parsed;
        }

        var limit = request.Limit ?? DefaultLimit;

        var ctx = await _guildChannelRepository.GetWithCallerRoleAsync(request.ChannelId, currentUserId, cancellationToken);
        if (ctx is null)
        {
            return ApplicationResponse<GetPinnedMessagesResponse>.Fail(
                ApplicationErrorCodes.Channel.NotFound,
                "Channel was not found");
        }

        if (ctx.Channel.Type != GuildChannelType.Text)
        {
            return ApplicationResponse<GetPinnedMessagesResponse>.Fail(
                ApplicationErrorCodes.Channel.NotText,
                "Pinned messages can only be listed in text channels");
        }

        if (ctx.CallerRole is null)
        {
            return ApplicationResponse<GetPinnedMessagesResponse>.Fail(
                ApplicationErrorCodes.Channel.AccessDenied,
                "You do not have access to this channel");
        }

        var page = await _pinnedMessageRepository.GetPinnedMessagesAsync(
            request.ChannelId,
            currentUserId,
            cursor,
            limit,
            cancellationToken);

        var attachmentsByMessageId = await _messageAttachmentRepository.GetByMessageIdsAsync(
            page.Items.Select(x => MessageId.From(x.MessageId)).ToArray(),
            cancellationToken);

        var items = page.Items
            .Select(x =>
            {
                attachmentsByMessageId.TryGetValue(MessageId.From(x.MessageId), out var attachments);
                return new GetPinnedMessagesItemResponse(
                    MessageId: x.MessageId,
                    AuthorUserId: x.AuthorUserId,
                    AuthorUsername: x.AuthorUsername,
                    AuthorDisplayName: x.AuthorDisplayName,
                    Content: x.Content,
                    Attachments: attachments?.Select(MessageAttachmentDto.FromDomain).ToArray()
                                 ?? Array.Empty<MessageAttachmentDto>(),
                    CreatedAtUtc: x.CreatedAtUtc,
                    UpdatedAtUtc: x.UpdatedAtUtc,
                    PinnedByUserId: x.PinnedByUserId,
                    PinnedAtUtc: x.PinnedAtUtc);
            })
            .ToArray();

        return ApplicationResponse<GetPinnedMessagesResponse>.Ok(
            new GetPinnedMessagesResponse(
                ChannelId: request.ChannelId.Value,
                Items: items,
                NextCursor: page.NextCursor is null
                    ? null
                    : PinnedMessagesCursorCodec.Encode(page.NextCursor)));
    }
}
