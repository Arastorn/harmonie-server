using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Harmonie.Application.Features.Auth.Register;
using Harmonie.Application.Features.Channels.SendMessage;
using Harmonie.Application.Features.Conversations.OpenConversation;
using Harmonie.Application.Features.Guilds.CreateChannel;
using Harmonie.Application.Features.Guilds.CreateGuild;
using Harmonie.Application.Features.Guilds.InviteMember;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using ChannelGetMessagesResponse = Harmonie.Application.Features.Channels.GetMessages.GetMessagesResponse;
using ConversationGetMessagesResponse = Harmonie.Application.Features.Conversations.GetMessages.GetMessagesResponse;
using ConversationSendMessageRequest = Harmonie.Application.Features.Conversations.SendMessage.SendMessageRequest;
using ConversationSendMessageResponse = Harmonie.Application.Features.Conversations.SendMessage.SendMessageResponse;

namespace Harmonie.API.IntegrationTests;

public sealed class GetMessagesReactionsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public GetMessagesReactionsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ─── Channel tests ──────────────────────────────────────────────

    [Fact]
    public async Task GetChannelMessages_WhenNoReactions_ShouldReturnEmptyReactionsArray()
    {
        var owner = await RegisterAsync();
        var (_, channelId) = await CreateGuildAndChannelAsync(owner.AccessToken);
        await SendChannelMessageAsync(channelId, "no reactions here", owner.AccessToken);

        var response = await SendAuthorizedGetAsync(
            $"/api/channels/{channelId}/messages",
            owner.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ChannelGetMessagesResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().ContainSingle();
        payload.Items[0].Reactions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetChannelMessages_WithReactions_ShouldIncludeReactionData()
    {
        var owner = await RegisterAsync();
        var (_, channelId) = await CreateGuildAndChannelAsync(owner.AccessToken);
        var message = await SendChannelMessageAsync(channelId, "react to this", owner.AccessToken);

        await SendAuthorizedPutAsync(
            $"/api/channels/{channelId}/messages/{message.MessageId}/reactions/%F0%9F%91%8D",
            owner.AccessToken);

        var response = await SendAuthorizedGetAsync(
            $"/api/channels/{channelId}/messages",
            owner.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ChannelGetMessagesResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().ContainSingle();

        var reactions = payload.Items[0].Reactions;
        reactions.Should().ContainSingle();
        reactions[0].Emoji.Should().Be("\U0001f44d");
        reactions[0].Count.Should().Be(1);
        reactions[0].ReactedByMe.Should().BeTrue();
    }

    [Fact]
    public async Task GetChannelMessages_ReactedByMe_ShouldReflectCallerPerspective()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var (guildId, channelId) = await CreateGuildAndChannelAsync(owner.AccessToken);
        await InviteMemberAsync(guildId, member.UserId, owner.AccessToken);

        var message = await SendChannelMessageAsync(channelId, "who reacted?", owner.AccessToken);

        // Owner reacts, member does not
        await SendAuthorizedPutAsync(
            $"/api/channels/{channelId}/messages/{message.MessageId}/reactions/%F0%9F%91%8D",
            owner.AccessToken);

        // Owner sees reactedByMe = true
        var ownerResponse = await SendAuthorizedGetAsync(
            $"/api/channels/{channelId}/messages",
            owner.AccessToken);
        var ownerPayload = await ownerResponse.Content.ReadFromJsonAsync<ChannelGetMessagesResponse>();
        ownerPayload!.Items[0].Reactions[0].ReactedByMe.Should().BeTrue();

        // Member sees reactedByMe = false
        var memberResponse = await SendAuthorizedGetAsync(
            $"/api/channels/{channelId}/messages",
            member.AccessToken);
        var memberPayload = await memberResponse.Content.ReadFromJsonAsync<ChannelGetMessagesResponse>();
        memberPayload!.Items[0].Reactions[0].ReactedByMe.Should().BeFalse();
        memberPayload.Items[0].Reactions[0].Count.Should().Be(1);
    }

    // ─── Conversation tests ─────────────────────────────────────────

    [Fact]
    public async Task GetConversationMessages_WhenNoReactions_ShouldReturnEmptyReactionsArray()
    {
        var caller = await RegisterAsync();
        var target = await RegisterAsync();
        var conversationId = await OpenConversationAsync(caller.AccessToken, target.UserId);
        await SendConversationMessageAsync(conversationId, "no reactions dm", caller.AccessToken);

        var response = await SendAuthorizedGetAsync(
            $"/api/conversations/{conversationId}/messages",
            caller.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ConversationGetMessagesResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().ContainSingle();
        payload.Items[0].Reactions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConversationMessages_WithReactions_ShouldIncludeReactionData()
    {
        var caller = await RegisterAsync();
        var target = await RegisterAsync();
        var conversationId = await OpenConversationAsync(caller.AccessToken, target.UserId);
        var message = await SendConversationMessageAsync(conversationId, "react dm", caller.AccessToken);

        await SendAuthorizedPutAsync(
            $"/api/conversations/{conversationId}/messages/{message.MessageId}/reactions/%E2%9D%A4",
            caller.AccessToken);

        var response = await SendAuthorizedGetAsync(
            $"/api/conversations/{conversationId}/messages",
            caller.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ConversationGetMessagesResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().ContainSingle();

        var reactions = payload.Items[0].Reactions;
        reactions.Should().ContainSingle();
        reactions[0].Emoji.Should().Be("\u2764");
        reactions[0].Count.Should().Be(1);
        reactions[0].ReactedByMe.Should().BeTrue();
    }

    [Fact]
    public async Task GetConversationMessages_ReactedByMe_ShouldReflectCallerPerspective()
    {
        var caller = await RegisterAsync();
        var target = await RegisterAsync();
        var conversationId = await OpenConversationAsync(caller.AccessToken, target.UserId);
        var message = await SendConversationMessageAsync(conversationId, "perspective dm", caller.AccessToken);

        // Caller reacts, target does not
        await SendAuthorizedPutAsync(
            $"/api/conversations/{conversationId}/messages/{message.MessageId}/reactions/%E2%9D%A4",
            caller.AccessToken);

        // Caller sees reactedByMe = true
        var callerResponse = await SendAuthorizedGetAsync(
            $"/api/conversations/{conversationId}/messages",
            caller.AccessToken);
        var callerPayload = await callerResponse.Content.ReadFromJsonAsync<ConversationGetMessagesResponse>();
        callerPayload!.Items[0].Reactions[0].ReactedByMe.Should().BeTrue();

        // Target sees reactedByMe = false
        var targetResponse = await SendAuthorizedGetAsync(
            $"/api/conversations/{conversationId}/messages",
            target.AccessToken);
        var targetPayload = await targetResponse.Content.ReadFromJsonAsync<ConversationGetMessagesResponse>();
        targetPayload!.Items[0].Reactions[0].ReactedByMe.Should().BeFalse();
        targetPayload.Items[0].Reactions[0].Count.Should().Be(1);
    }

    // ─── Helpers ────────────────────────────────────────────────────

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

    private async Task<(string GuildId, string ChannelId)> CreateGuildAndChannelAsync(string accessToken)
    {
        var guildName = $"guild{Guid.NewGuid():N}"[..16];
        var createGuildResponse = await SendAuthorizedPostAsync(
            "/api/guilds",
            new CreateGuildRequest(guildName),
            accessToken);
        createGuildResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var guildPayload = await createGuildResponse.Content.ReadFromJsonAsync<CreateGuildResponse>();
        guildPayload.Should().NotBeNull();

        var createChannelResponse = await SendAuthorizedPostAsync(
            $"/api/guilds/{guildPayload!.GuildId}/channels",
            new CreateChannelRequest($"chan{Guid.NewGuid():N}"[..16], ChannelTypeInput.Text, 1),
            accessToken);
        createChannelResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var channelPayload = await createChannelResponse.Content.ReadFromJsonAsync<CreateChannelResponse>();
        channelPayload.Should().NotBeNull();

        return (guildPayload.GuildId, channelPayload!.ChannelId);
    }

    private async Task InviteMemberAsync(string guildId, string userId, string ownerAccessToken)
    {
        var response = await SendAuthorizedPostAsync(
            $"/api/guilds/{guildId}/members/invite",
            new InviteMemberRequest(userId),
            ownerAccessToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<SendMessageResponse> SendChannelMessageAsync(
        string channelId,
        string content,
        string accessToken)
    {
        var response = await SendAuthorizedPostAsync(
            $"/api/channels/{channelId}/messages",
            new SendMessageRequest(content),
            accessToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<SendMessageResponse>();
        payload.Should().NotBeNull();
        return payload!;
    }

    private async Task<string> OpenConversationAsync(string accessToken, string targetUserId)
    {
        var response = await SendAuthorizedPostAsync(
            "/api/conversations",
            new OpenConversationRequest(targetUserId),
            accessToken);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<OpenConversationResponse>();
        payload.Should().NotBeNull();
        return payload!.ConversationId;
    }

    private async Task<ConversationSendMessageResponse> SendConversationMessageAsync(
        string conversationId,
        string content,
        string accessToken)
    {
        var response = await SendAuthorizedPostAsync(
            $"/api/conversations/{conversationId}/messages",
            new ConversationSendMessageRequest(content),
            accessToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<ConversationSendMessageResponse>();
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
            Content = JsonContent.Create(payload, options: JsonOptions)
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

    private async Task<HttpResponseMessage> SendAuthorizedPutAsync(
        string uri,
        string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }
}
