using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Harmonie.Application.Common;
using Harmonie.Application.Features.Auth.Register;
using Harmonie.Application.Features.Conversations.OpenConversation;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Harmonie.API.IntegrationTests;

public sealed class ConversationEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ConversationEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OpenConversation_FirstRequest_ShouldCreateConversation()
    {
        var caller = await RegisterAsync();
        var target = await RegisterAsync();

        var response = await SendAuthorizedPostAsync(
            "/api/conversations",
            new OpenConversationRequest(target.UserId),
            caller.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<OpenConversationResponse>();
        payload.Should().NotBeNull();
        payload!.Created.Should().BeTrue();
        payload.ConversationId.Should().NotBeNullOrWhiteSpace();
        payload.User1Id.Should().NotBe(payload.User2Id);
    }

    [Fact]
    public async Task OpenConversation_SecondRequestForSamePair_ShouldReturnExistingConversation()
    {
        var caller = await RegisterAsync();
        var target = await RegisterAsync();

        var firstResponse = await SendAuthorizedPostAsync(
            "/api/conversations",
            new OpenConversationRequest(target.UserId),
            caller.AccessToken);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<OpenConversationResponse>();
        firstPayload.Should().NotBeNull();

        var secondResponse = await SendAuthorizedPostAsync(
            "/api/conversations",
            new OpenConversationRequest(caller.UserId),
            target.AccessToken);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondPayload = await secondResponse.Content.ReadFromJsonAsync<OpenConversationResponse>();
        secondPayload.Should().NotBeNull();
        secondPayload!.Created.Should().BeFalse();
        secondPayload.ConversationId.Should().Be(firstPayload!.ConversationId);
    }

    [Fact]
    public async Task OpenConversation_WhenTargetUserDoesNotExist_ShouldReturnNotFound()
    {
        var caller = await RegisterAsync();

        var response = await SendAuthorizedPostAsync(
            "/api/conversations",
            new OpenConversationRequest(Guid.NewGuid().ToString()),
            caller.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.User.NotFound);
    }

    [Fact]
    public async Task OpenConversation_WhenCallerTargetsSelf_ShouldReturnBadRequest()
    {
        var caller = await RegisterAsync();

        var response = await SendAuthorizedPostAsync(
            "/api/conversations",
            new OpenConversationRequest(caller.UserId),
            caller.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Conversation.CannotOpenSelf);
    }

    [Fact]
    public async Task OpenConversation_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/conversations",
            new OpenConversationRequest(Guid.NewGuid().ToString()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
}
