using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Harmonie.Application.Common;
using Harmonie.Application.Features.Auth.Register;
using Harmonie.Application.Features.Channels.SendMessage;
using Harmonie.Application.Features.Guilds.CreateGuild;
using Harmonie.Application.Features.Guilds.GetGuildChannels;
using Harmonie.Application.Features.Guilds.InviteMember;
using Harmonie.Application.Features.Guilds.ListUserGuilds;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Harmonie.API.IntegrationTests;

public sealed class DeleteGuildEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DeleteGuildEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DeleteGuild_WhenOwnerDeletesGuild_ShouldReturn204AndCascadeDataRemoval()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();

        var createGuildResponse = await SendAuthorizedPostAsync(
            "/api/guilds",
            new CreateGuildRequest("Delete Endpoint Guild"),
            owner.AccessToken);
        createGuildResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createGuildPayload = await createGuildResponse.Content.ReadFromJsonAsync<CreateGuildResponse>();
        createGuildPayload.Should().NotBeNull();

        var inviteResponse = await SendAuthorizedPostAsync(
            $"/api/guilds/{createGuildPayload!.GuildId}/members/invite",
            new InviteMemberRequest(member.UserId),
            owner.AccessToken);
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var channelsResponse = await SendAuthorizedGetAsync(
            $"/api/guilds/{createGuildPayload.GuildId}/channels",
            owner.AccessToken);
        channelsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var channelsPayload = await channelsResponse.Content.ReadFromJsonAsync<GetGuildChannelsResponse>();
        channelsPayload.Should().NotBeNull();

        var textChannel = channelsPayload!.Channels.First(channel => channel.Type == "Text");

        var sendMessageResponse = await SendAuthorizedPostAsync(
            $"/api/channels/{textChannel.ChannelId}/messages",
            new SendMessageRequest("guild delete cascade"),
            owner.AccessToken);
        sendMessageResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var deleteGuildResponse = await SendAuthorizedDeleteAsync(
            $"/api/guilds/{createGuildPayload.GuildId}",
            owner.AccessToken);
        deleteGuildResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deletedChannelsResponse = await SendAuthorizedGetAsync(
            $"/api/guilds/{createGuildPayload.GuildId}/channels",
            owner.AccessToken);
        deletedChannelsResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var deletedMessagesResponse = await SendAuthorizedGetAsync(
            $"/api/channels/{textChannel.ChannelId}/messages",
            owner.AccessToken);
        deletedMessagesResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownerGuildsResponse = await SendAuthorizedGetAsync("/api/guilds", owner.AccessToken);
        ownerGuildsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var ownerGuildsPayload = await ownerGuildsResponse.Content.ReadFromJsonAsync<ListUserGuildsResponse>();
        ownerGuildsPayload.Should().NotBeNull();
        ownerGuildsPayload!.Guilds.Should().NotContain(guild => guild.GuildId == createGuildPayload.GuildId);

        var memberGuildsResponse = await SendAuthorizedGetAsync("/api/guilds", member.AccessToken);
        memberGuildsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var memberGuildsPayload = await memberGuildsResponse.Content.ReadFromJsonAsync<ListUserGuildsResponse>();
        memberGuildsPayload.Should().NotBeNull();
        memberGuildsPayload!.Guilds.Should().NotContain(guild => guild.GuildId == createGuildPayload.GuildId);
    }

    [Fact]
    public async Task DeleteGuild_WhenCallerIsNotOwner_ShouldReturn403()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();

        var createGuildResponse = await SendAuthorizedPostAsync(
            "/api/guilds",
            new CreateGuildRequest("Delete Forbidden Guild"),
            owner.AccessToken);
        createGuildResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createGuildPayload = await createGuildResponse.Content.ReadFromJsonAsync<CreateGuildResponse>();
        createGuildPayload.Should().NotBeNull();

        var inviteResponse = await SendAuthorizedPostAsync(
            $"/api/guilds/{createGuildPayload!.GuildId}/members/invite",
            new InviteMemberRequest(member.UserId),
            owner.AccessToken);
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteGuildResponse = await SendAuthorizedDeleteAsync(
            $"/api/guilds/{createGuildPayload.GuildId}",
            member.AccessToken);
        deleteGuildResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var error = await deleteGuildResponse.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Guild.AccessDenied);
    }

    [Fact]
    public async Task DeleteGuild_WhenGuildNotFound_ShouldReturn404()
    {
        var user = await RegisterAsync();

        var deleteGuildResponse = await SendAuthorizedDeleteAsync(
            $"/api/guilds/{Guid.NewGuid()}",
            user.AccessToken);
        deleteGuildResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await deleteGuildResponse.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Guild.NotFound);
    }

    [Fact]
    public async Task DeleteGuild_WhenNotAuthenticated_ShouldReturn401()
    {
        var createGuildResponse = await _client.DeleteAsync($"/api/guilds/{Guid.NewGuid()}");
        createGuildResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<RegisterResponse> RegisterAsync()
    {
        var request = new RegisterRequest(
            Email: $"test{Guid.NewGuid():N}@harmonie.chat",
            Username: $"user{Guid.NewGuid():N}"[..20],
            Password: "Test123!@#");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        payload.Should().NotBeNull();
        return payload!;
    }

    private async Task<HttpResponseMessage> SendAuthorizedPostAsync<TRequest>(
        string uri,
        TRequest payload,
        string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create(payload)
        };
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

    private async Task<HttpResponseMessage> SendAuthorizedDeleteAsync(
        string uri,
        string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }
}
