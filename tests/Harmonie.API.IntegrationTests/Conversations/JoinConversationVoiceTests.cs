using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Harmonie.API.IntegrationTests.Common;
using Harmonie.Application.Common;
using Harmonie.Application.Features.Conversations.JoinConversationVoice;
using Harmonie.Application.Features.Conversations.OpenConversation;
using Xunit;

namespace Harmonie.API.IntegrationTests.Conversations;

public sealed class JoinConversationVoiceTests : IClassFixture<HarmonieWebApplicationFactory>
{
    private readonly HttpClient _client;

    public JoinConversationVoiceTests(HarmonieWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task JoinConversationVoice_WhenParticipantJoins_ShouldReturn200WithToken()
    {
        var caller = await AuthTestHelper.RegisterAsync(_client);
        var target = await AuthTestHelper.RegisterAsync(_client);
        var conversationId = await OpenConversationAndGetIdAsync(caller.AccessToken, target.UserId);

        var response = await _client.SendAuthorizedPostNoBodyAsync(
            $"/api/conversations/{conversationId}/voice/join",
            caller.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JoinConversationVoiceResponse>(TestContext.Current.CancellationToken);
        payload.Should().NotBeNull();
        payload!.Token.Should().NotBeNullOrWhiteSpace();
        payload.RoomName.Should().Be($"conversation:{conversationId}");
        payload.CurrentParticipants.Should().NotBeNull();
    }

    [Fact]
    public async Task JoinConversationVoice_WhenUserIsNotParticipant_ShouldReturn403()
    {
        var caller = await AuthTestHelper.RegisterAsync(_client);
        var target = await AuthTestHelper.RegisterAsync(_client);
        var outsider = await AuthTestHelper.RegisterAsync(_client);
        var conversationId = await OpenConversationAndGetIdAsync(caller.AccessToken, target.UserId);

        var response = await _client.SendAuthorizedPostNoBodyAsync(
            $"/api/conversations/{conversationId}/voice/join",
            outsider.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var error = await response.Content.ReadFromJsonAsync<ApplicationError>(TestContext.Current.CancellationToken);
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Conversation.VoiceAccessDenied);
    }

    [Fact]
    public async Task JoinConversationVoice_WhenConversationDoesNotExist_ShouldReturn404()
    {
        var user = await AuthTestHelper.RegisterAsync(_client);

        var response = await _client.SendAuthorizedPostNoBodyAsync(
            $"/api/conversations/{Guid.NewGuid()}/voice/join",
            user.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<ApplicationError>(TestContext.Current.CancellationToken);
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Conversation.NotFound);
    }

    [Fact]
    public async Task JoinConversationVoice_WhenNotAuthenticated_ShouldReturn401()
    {
        var response = await _client.PostAsync(
            $"/api/conversations/{Guid.NewGuid()}/voice/join",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> OpenConversationAndGetIdAsync(string accessToken, Guid targetUserId)
    {
        var response = await _client.SendAuthorizedPostAsync(
            "/api/conversations",
            new OpenConversationRequest(targetUserId),
            accessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<OpenConversationResponse>(TestContext.Current.CancellationToken);
        return payload!.ConversationId;
    }
}
