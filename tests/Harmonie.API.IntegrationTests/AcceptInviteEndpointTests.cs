using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Harmonie.Application.Common;
using Harmonie.Application.Features.Auth.Register;
using Harmonie.Application.Features.Guilds.AcceptInvite;
using Harmonie.Application.Features.Guilds.CreateGuild;
using Harmonie.Application.Features.Guilds.CreateGuildInvite;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Harmonie.API.IntegrationTests;

public sealed class AcceptInviteEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public AcceptInviteEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AcceptInvite_WithValidCode_ShouldReturn200()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        var owner = await RegisterAsync(token);
        var joiner = await RegisterAsync(token + "j");

        var guild = await CreateGuildAsync($"AcceptGuild{token}", owner.AccessToken);
        var invite = await CreateInviteAsync(guild.GuildId, owner.AccessToken);

        var response = await SendAuthorizedPostAsync(
            $"/api/invites/{invite.Code}/accept",
            joiner.AccessToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AcceptInviteResponse>();
        result.Should().NotBeNull();
        result!.GuildId.Should().Be(guild.GuildId);
        result.Role.Should().Be("Member");
    }

    [Fact]
    public async Task AcceptInvite_WhenAlreadyMember_ShouldReturn409()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        var owner = await RegisterAsync(token);

        var guild = await CreateGuildAsync($"AlreadyMem{token}", owner.AccessToken);
        var invite = await CreateInviteAsync(guild.GuildId, owner.AccessToken);

        // Owner is already a member
        var response = await SendAuthorizedPostAsync(
            $"/api/invites/{invite.Code}/accept",
            owner.AccessToken);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await response.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Guild.MemberAlreadyExists);
    }

    [Fact]
    public async Task AcceptInvite_WhenInviteNotFound_ShouldReturn404()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        var user = await RegisterAsync(token);

        var response = await SendAuthorizedPostAsync(
            "/api/invites/ZZZZZZZZ/accept",
            user.AccessToken);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Invite.NotFound);
    }

    [Fact]
    public async Task AcceptInvite_WhenUnauthenticated_ShouldReturn401()
    {
        var response = await _client.PostAsync("/api/invites/ABCD1234/accept", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AcceptInvite_WhenInvalidCodeFormat_ShouldReturn400()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        var user = await RegisterAsync(token);

        var response = await SendAuthorizedPostAsync(
            "/api/invites/abc/accept",
            user.AccessToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Common.ValidationFailed);
    }

    [Fact]
    public async Task AcceptInvite_WithMaxUsesReached_ShouldReturn410()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        var owner = await RegisterAsync(token);
        var joiner1 = await RegisterAsync(token + "j1");
        var joiner2 = await RegisterAsync(token + "j2");

        var guild = await CreateGuildAsync($"MaxUseGuild{token}", owner.AccessToken);
        var invite = await CreateInviteAsync(guild.GuildId, owner.AccessToken, maxUses: 1);

        // First accept should succeed
        var response1 = await SendAuthorizedPostAsync(
            $"/api/invites/{invite.Code}/accept",
            joiner1.AccessToken);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second accept should fail — max uses reached
        var response2 = await SendAuthorizedPostAsync(
            $"/api/invites/{invite.Code}/accept",
            joiner2.AccessToken);
        response2.StatusCode.Should().Be(HttpStatusCode.Gone);

        var error = await response2.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Invite.Exhausted);
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

    private async Task<CreateGuildInviteResponse> CreateInviteAsync(
        string guildId,
        string accessToken,
        int? maxUses = null)
    {
        var response = await SendAuthorizedPostAsync(
            $"/api/guilds/{guildId}/invites",
            new CreateGuildInviteRequest(MaxUses: maxUses),
            accessToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var invite = await response.Content.ReadFromJsonAsync<CreateGuildInviteResponse>();
        invite.Should().NotBeNull();
        return invite!;
    }

    private async Task<HttpResponseMessage> SendAuthorizedPostAsync(
        string uri,
        string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
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
}
