using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.Entities.Messages;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Channels.Reads;

public sealed class ChannelReadScope : IReadScope<ChannelReadScope.Context>
{
    public sealed record Context : ScopeContext;

    private readonly GuildChannelId _channelId;
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IChannelReadStateRepository _channelReadStateRepository;

    public ChannelReadScope(
        GuildChannelId channelId,
        IGuildChannelRepository guildChannelRepository,
        IMessageRepository messageRepository,
        IChannelReadStateRepository channelReadStateRepository)
    {
        _channelId = channelId;
        _guildChannelRepository = guildChannelRepository;
        _messageRepository = messageRepository;
        _channelReadStateRepository = channelReadStateRepository;
    }

    public async Task<AuthorizationResult<Context>> AuthorizeAsync(UserId caller, CancellationToken ct)
    {
        var result = await ChannelScopeAuthorizer.AuthorizeAsync(_guildChannelRepository, _channelId, caller, ct);
        if (result is ChannelAuthResult.Denied denied)
            return new AuthorizationResult<Context>.Denied(denied.Error);

        return new AuthorizationResult<Context>.Authorized(new Context());
    }

    public Task<MessageId?> GetLatestMessageIdAsync(CancellationToken ct)
        => _messageRepository.GetLatestChannelMessageIdAsync(_channelId, ct);

    public Task UpsertReadStateAsync(MessageReadState state, CancellationToken ct)
        => _channelReadStateRepository.UpsertAsync(state, ct);
}
