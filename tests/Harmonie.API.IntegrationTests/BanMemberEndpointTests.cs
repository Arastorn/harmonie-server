using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Harmonie.Application.Common;
using Harmonie.Application.Features.Auth.Register;
using Harmonie.Application.Features.Guilds.AcceptInvite;
using Harmonie.Application.Features.Guilds.BanMember;
using Harmonie.Application.Features.Guilds.CreateGuild;
using Harmonie.Application.Features.Guilds.CreateGuildInvite;
using Harmonie.Application.Features.Guilds.GetGuildChannels;
using Harmonie.Application.Features.Guilds.GetGuildMembers;
using Harmonie.Application.Features.Guilds.InviteMember;
using Harmonie.Application.Features.Channels.SendMessage;
using Harmonie.Application.Features.Channels.GetMessages;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Harmonie.API.IntegrationTests;

public sealed class BanMemberEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public BanMemberEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task BanMember_WhenAdminBansMember_ShouldReturn201AndRemoveFromGuild()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        var owner = await RegisterAsync(token);
        var member = await RegisterAsync(token + "m");

        var guild = await CreateGuildAsync($"BanGuild{token}", owner.AccessToken);

        await InviteMemberAsync(guild.GuildId, member.UserId, owner.AccessToken);

        var banResponse = await SendAuthorizedPostAsync(
            $"/api/guilds/{guild.GuildId}/bans",
            new BanMemberRequest(member.UserId, "Spamming"),
            owner.AccessToken);
        banResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var banPayload = await banResponse.Content.ReadFromJsonAsync<BanMemberResponse>();
        banPayload.Should().NotBeNull();
        banPayload!.GuildId.Should().Be(guild.GuildId);
        banPayload.UserId.Should().Be(member.UserId);
        banPayload.Reason.Should().Be("Spamming");
        banPayload.BannedBy.Should().Be(owner.UserId);

        // Verify member was removed from guild
        var membersResponse = await SendAuthorizedGetAsync(
            $"/api/guilds/{guild.GuildId}/members",
            owner.AccessToken);
        membersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var membersPayload = await membersResponse.Content.ReadFromJsonAsync<GetGuildMembersResponse>();
        membersPayload.Should().NotBeNull();
        membersPayload!.Members.Should().NotContain(m => m.UserId == member.UserId);
    }

    [Fact]
    public async Task BanMember_WhenBannedUserTriesAcceptInvite_ShouldReturn403()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        var owner = await RegisterAsync(token);
        var banned = await RegisterAsync(token + "b");

        var guild = await CreateGuildAsync($"BanInvite{token}", owner.AccessToken);

        await InviteMemberAsync(guild.GuildId, banned.UserId, owner.AccessToken);

        // Ban the member
        var banResponse = await SendAuthorizedPostAsync(
            $"/api/guilds/{guild.GuildId}/bans",
            new BanMemberRequest(banned.UserId),
            owner.AccessToken);
        banResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Create a new invite link
        var invite = await CreateInviteAsync(guild.GuildId, owner.AccessToken);

        // Banned user tries to accept
        var acceptResponse = await SendAuthorizedPostNoBodyAsync(
            $"/api/invites/{invite.Code}/accept",
            banned.AccessToken);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var error = await acceptResponse.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Guild.UserBanned);
    }

    [Fact]
    public async Task BanMember_WhenNonAdmin_ShouldReturn403()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        var owner = await RegisterAsync(token);
        var member = await RegisterAsync(token + "m");
        var target = await RegisterAsync(token + "t");

        var guild = await CreateGuildAsync($"NonAdmBan{token}", owner.AccessToken);

        await InviteMemberAsync(guild.GuildId, member.UserId, owner.AccessToken);
        await InviteMemberAsync(guild.GuildId, target.UserId, owner.AccessToken);

        var banResponse = await SendAuthorizedPostAsync(
            $"/api/guilds/{guild.GuildId}/bans",
            new BanMemberRequest(target.UserId),
            member.AccessToken);
        banResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var error = await banResponse.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Guild.AccessDenied);
    }

    [Fact]
    public async Task BanMember_WhenBanOwner_ShouldReturn409()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        var owner = await RegisterAsync(token);

        var guild = await CreateGuildAsync($"BanOwner{token}", owner.AccessToken);

        // Owner tries to ban themselves (owner check comes before self-ban check in handler)
        // Let's use a second admin to try to ban the owner
        var admin = await RegisterAsync(token + "a");
        await InviteMemberAsync(guild.GuildId, admin.UserId, owner.AccessToken);

        // Promote to admin
        await SendAuthorizedPutAsync(
            $"/api/guilds/{guild.GuildId}/members/{admin.UserId}/role",
            new { Role = "Admin" },
            owner.AccessToken);

        var banResponse = await SendAuthorizedPostAsync(
            $"/api/guilds/{guild.GuildId}/bans",
            new BanMemberRequest(owner.UserId),
            admin.AccessToken);
        banResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await banResponse.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Guild.OwnerCannotBeBanned);
    }

    [Fact]
    public async Task BanMember_WhenAlreadyBanned_ShouldReturn409()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        var owner = await RegisterAsync(token);
        var member = await RegisterAsync(token + "m");

        var guild = await CreateGuildAsync($"DblBan{token}", owner.AccessToken);

        await InviteMemberAsync(guild.GuildId, member.UserId, owner.AccessToken);

        var firstBan = await SendAuthorizedPostAsync(
            $"/api/guilds/{guild.GuildId}/bans",
            new BanMemberRequest(member.UserId),
            owner.AccessToken);
        firstBan.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondBan = await SendAuthorizedPostAsync(
            $"/api/guilds/{guild.GuildId}/bans",
            new BanMemberRequest(member.UserId),
            owner.AccessToken);
        secondBan.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await secondBan.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Guild.AlreadyBanned);
    }

    [Fact]
    public async Task BanMember_WithPurge_ShouldSoftDeleteMessages()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        var owner = await RegisterAsync(token);
        var member = await RegisterAsync(token + "m");

        var guild = await CreateGuildAsync($"PurgeBan{token}", owner.AccessToken);

        await InviteMemberAsync(guild.GuildId, member.UserId, owner.AccessToken);

        // Get text channel
        var channelsResponse = await SendAuthorizedGetAsync(
            $"/api/guilds/{guild.GuildId}/channels",
            owner.AccessToken);
        var channels = await channelsResponse.Content.ReadFromJsonAsync<GetGuildChannelsResponse>();
        var textChannel = channels!.Channels.First(c => c.Type == "Text");

        // Member sends a message
        var sendResponse = await SendAuthorizedPostAsync(
            $"/api/channels/{textChannel.ChannelId}/messages",
            new SendMessageRequest($"Hello from {token}"),
            member.AccessToken);
        sendResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Ban with purge
        var banResponse = await SendAuthorizedPostAsync(
            $"/api/guilds/{guild.GuildId}/bans",
            new BanMemberRequest(member.UserId, PurgeMessagesDays: 7),
            owner.AccessToken);
        banResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify messages are soft-deleted (owner should see no messages from banned user)
        var messagesResponse = await SendAuthorizedGetAsync(
            $"/api/channels/{textChannel.ChannelId}/messages",
            owner.AccessToken);
        messagesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var messages = await messagesResponse.Content.ReadFromJsonAsync<GetMessagesResponse>();
        messages.Should().NotBeNull();
        messages!.Items.Should().NotContain(m => m.AuthorUserId == member.UserId);
    }

    [Fact]
    public async Task BanMember_WhenNotAuthenticated_ShouldReturn401()
    {
        var nonExistentGuildId = Guid.NewGuid();

        var banResponse = await _client.PostAsJsonAsync(
            $"/api/guilds/{nonExistentGuildId}/bans",
            new BanMemberRequest(Guid.NewGuid().ToString()));
        banResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<RegisterResponse> RegisterAsync(string token)
    {
        var request = new RegisterRequest(
            Email: $"test{token}{Guid.NewGuid():N}@harmonie.chat",
            Username: $"u{token}{Guid.NewGuid():N}"[..20],
            Password: "Test123!@#");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        payload.Should().NotBeNull();
        return payload!;
    }

    private async Task<CreateGuildResponse> CreateGuildAsync(string name, string accessToken)
    {
        var response = await SendAuthorizedPostAsync(
            "/api/guilds",
            new CreateGuildRequest(name),
            accessToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var guild = await response.Content.ReadFromJsonAsync<CreateGuildResponse>();
        guild.Should().NotBeNull();
        return guild!;
    }

    private async Task InviteMemberAsync(string guildId, string userId, string accessToken)
    {
        var response = await SendAuthorizedPostAsync(
            $"/api/guilds/{guildId}/members/invite",
            new InviteMemberRequest(userId),
            accessToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<CreateGuildInviteResponse> CreateInviteAsync(
        string guildId,
        string accessToken)
    {
        var response = await SendAuthorizedPostAsync(
            $"/api/guilds/{guildId}/invites",
            new CreateGuildInviteRequest(),
            accessToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var invite = await response.Content.ReadFromJsonAsync<CreateGuildInviteResponse>();
        invite.Should().NotBeNull();
        return invite!;
    }

    private async Task<HttpResponseMessage> SendAuthorizedPostAsync<TRequest>(
        string uri,
        TRequest payload,
        string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create(payload, options: _jsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendAuthorizedPostNoBodyAsync(
        string uri,
        string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendAuthorizedGetAsync(
        string uri,
        string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendAuthorizedPutAsync<TRequest>(
        string uri,
        TRequest payload,
        string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = JsonContent.Create(payload, options: _jsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }
}
