using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Application.Interfaces.Guilds;
using Harmonie.Domain.Common;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Channels.Messages;

/// <summary>
/// Channel-specific implementation of <see cref="IMessageEditDeleteScope{TContext}"/>.
/// </summary>
public sealed class ChannelMessageEditDeleteScope : IMessageEditDeleteScope<ChannelMessageEditDeleteScope.Context>
{
    public sealed record Context(
        GuildChannelId ChannelId,
        string ChannelName,
        GuildId GuildId,
        string GuildName,
        GuildRole? CallerRole) : ScopeContext;

    private readonly GuildChannelId _channelId;
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IGuildMemberRepository _guildMemberRepository;
    private readonly IMessageEventPublisher _messageEventPublisher;
    private readonly ILogger<ChannelMessageEditDeleteScope> _logger;

    public ChannelMessageEditDeleteScope(
        GuildChannelId channelId,
        IGuildChannelRepository guildChannelRepository,
        IGuildMemberRepository guildMemberRepository,
        IMessageEventPublisher messageEventPublisher,
        ILogger<ChannelMessageEditDeleteScope> logger)
    {
        _channelId = channelId;
        _guildChannelRepository = guildChannelRepository;
        _guildMemberRepository = guildMemberRepository;
        _messageEventPublisher = messageEventPublisher;
        _logger = logger;
    }

    public async Task<AuthorizationResult<Context>> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var result = await ChannelScopeAuthorizer.AuthorizeAsync(_guildChannelRepository, _channelId, caller, ct);
        if (result is ChannelAuthResult.Denied denied)
            return new AuthorizationResult<Context>.Denied(denied.Error);

        var access = ((ChannelAuthResult.Authorized)result).Context;
        return new AuthorizationResult<Context>.Authorized(new Context(
            _channelId,
            access.Channel.Name,
            access.Channel.GuildId,
            access.GuildName ?? string.Empty,
            access.CallerRole));
    }

    // In channels, admins can delete any message, not just their own.
    public bool CanDeleteOthersMessages(Context context)
        => context.CallerRole == GuildRole.Admin;

    public async Task<Result> ValidateMentionedUsersAsync(
        IReadOnlyCollection<UserId> userIds,
        Context context,
        CancellationToken ct)
    {
        if (userIds.Count == 0)
            return Result.Success();

        var memberSet = await _guildMemberRepository.GetMembersInAsync(context.GuildId, userIds, ct);
        var nonMembers = userIds.Where(id => !memberSet.Contains(id)).ToArray();
        if (nonMembers.Length > 0)
        {
            return Result.Failure($"Users not members of guild {context.GuildId.Value}: {string.Join(", ", nonMembers.Select(id => id.Value))}");
        }

        return Result.Success();
    }

    public async Task NotifyMessageUpdatedAsync(
        Context context,
        MessageId messageId,
        string? content,
        IReadOnlyList<Guid> mentionedUserIds,
        DateTime updatedAtUtc,
        CancellationToken ct)
    {
        await _messageEventPublisher.PublishUpdatedAsync(
            new MessageUpdatedEventEnvelope(
                messageId,
                new MessageEventLocation.Channel(
                    context.ChannelId,
                    context.ChannelName,
                    context.GuildId,
                    context.GuildName),
                content,
                mentionedUserIds,
                updatedAtUtc),
            CancellationToken.None);
    }

    public async Task NotifyMessageDeletedAsync(
        Context context,
        MessageId messageId,
        CancellationToken ct)
    {
        await _messageEventPublisher.PublishDeletedAsync(
            new MessageDeletedEventEnvelope(
                messageId,
                new MessageEventLocation.Channel(
                    context.ChannelId,
                    context.ChannelName,
                    context.GuildId,
                    context.GuildName)),
            CancellationToken.None);
    }
}
