using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Common;

/// <summary>
/// Discriminated union for channel scope authorization results.
/// </summary>
public abstract record ChannelAuthResult
{
    private ChannelAuthResult() { }
    public sealed record Authorized(ChannelAccessContext Context) : ChannelAuthResult;
    public sealed record Denied(ApplicationError Error) : ChannelAuthResult;
}

/// <summary>
/// Shared authorization logic for channel scopes. Eliminates the duplicated
/// GetWithCallerRoleAsync + 3 checks present in every channel scope.
/// Each scope maps the returned <see cref="ChannelAccessContext"/> to its own Context type.
/// </summary>
public static class ChannelScopeAuthorizer
{
    public static async Task<ChannelAuthResult> AuthorizeAsync(
        IGuildChannelRepository repository,
        GuildChannelId channelId,
        UserId caller,
        CancellationToken ct)
    {
        var ctx = await repository.GetWithCallerRoleAsync(channelId, caller, ct);
        if (ctx is null)
            return new ChannelAuthResult.Denied(
                new ApplicationError(ApplicationErrorCodes.Channel.NotFound, "Channel was not found"));

        if (ctx.Channel.Type != GuildChannelType.Text)
            return new ChannelAuthResult.Denied(
                new ApplicationError(ApplicationErrorCodes.Channel.NotText, "Messages can only be used in text channels"));

        if (ctx.CallerRole is null)
            return new ChannelAuthResult.Denied(
                new ApplicationError(ApplicationErrorCodes.Channel.AccessDenied, "You do not have access to this channel"));

        return new ChannelAuthResult.Authorized(ctx);
    }
}
