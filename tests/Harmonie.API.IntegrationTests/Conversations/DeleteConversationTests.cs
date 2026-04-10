using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Harmonie.API.IntegrationTests.Common;
using Harmonie.Application.Common;
using Xunit;

namespace Harmonie.API.IntegrationTests.Conversations;

public sealed class DeleteConversationTests : IClassFixture<HarmonieWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DeleteConversationTests(HarmonieWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DeleteConversation_WhenParticipantDeletes_ShouldReturn204()
    {
        var caller = await AuthTestHelper.RegisterAsync(_client);
        var other = await AuthTestHelper.RegisterAsync(_client);
        var conversationId = await ConversationTestHelper.OpenConversationAsync(_client, caller.AccessToken, other.UserId);

        var response = await _client.SendAuthorizedDeleteAsync(
            $"/api/conversations/{conversationId}",
            caller.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteConversation_WhenConversationDoesNotExist_ShouldReturnNotFound()
    {
        var caller = await AuthTestHelper.RegisterAsync(_client);

        var response = await _client.SendAuthorizedDeleteAsync(
            $"/api/conversations/{Guid.NewGuid()}",
            caller.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Conversation.NotFound);
    }

    [Fact]
    public async Task DeleteConversation_WhenCallerIsNotParticipant_ShouldReturnForbidden()
    {
        var participantOne = await AuthTestHelper.RegisterAsync(_client);
        var participantTwo = await AuthTestHelper.RegisterAsync(_client);
        var outsider = await AuthTestHelper.RegisterAsync(_client);
        var conversationId = await ConversationTestHelper.OpenConversationAsync(_client, participantOne.AccessToken, participantTwo.UserId);

        var response = await _client.SendAuthorizedDeleteAsync(
            $"/api/conversations/{conversationId}",
            outsider.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var error = await response.Content.ReadFromJsonAsync<ApplicationError>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ApplicationErrorCodes.Conversation.AccessDenied);
    }

    [Fact]
    public async Task DeleteConversation_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await _client.DeleteAsync($"/api/conversations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteConversation_WhenLastParticipantDeletes_ConversationShouldBeGone()
    {
        var caller = await AuthTestHelper.RegisterAsync(_client);
        var other = await AuthTestHelper.RegisterAsync(_client);
        var conversationId = await ConversationTestHelper.OpenConversationAsync(_client, caller.AccessToken, other.UserId);

        // Both participants delete the conversation
        var firstDelete = await _client.SendAuthorizedDeleteAsync(
            $"/api/conversations/{conversationId}",
            caller.AccessToken);
        firstDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondDelete = await _client.SendAuthorizedDeleteAsync(
            $"/api/conversations/{conversationId}",
            other.AccessToken);
        secondDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Conversation should now be gone for both
        var getResponse = await _client.SendAuthorizedDeleteAsync(
            $"/api/conversations/{conversationId}",
            caller.AccessToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteConversation_WhenOneParticipantLeaves_OtherShouldStillHaveAccess()
    {
        var caller = await AuthTestHelper.RegisterAsync(_client);
        var other = await AuthTestHelper.RegisterAsync(_client);
        var conversationId = await ConversationTestHelper.OpenConversationAsync(_client, caller.AccessToken, other.UserId);

        // Caller deletes their side
        var deleteResponse = await _client.SendAuthorizedDeleteAsync(
            $"/api/conversations/{conversationId}",
            caller.AccessToken);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Other participant should still be able to fetch messages
        var messagesResponse = await _client.SendAuthorizedGetAsync(
            $"/api/conversations/{conversationId}/messages",
            other.AccessToken);
        messagesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteConversation_AfterLeavingGroupConversation_OtherParticipantsShouldStillHaveAccess()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var callerReg = await AuthTestHelper.RegisterAsync(_client, $"leaver_{tag}");
        var other1Reg = await AuthTestHelper.RegisterAsync(_client, $"remain1_{tag}");
        var other2Reg = await AuthTestHelper.RegisterAsync(_client, $"remain2_{tag}");

        var groupConversation = await ConversationTestHelper.CreateGroupConversationAsync(
            _client,
            callerReg.AccessToken,
            "Test group",
            [callerReg.UserId, other1Reg.UserId, other2Reg.UserId]);

        var conversationId = groupConversation.ConversationId;

        // Caller leaves the group
        var deleteResponse = await _client.SendAuthorizedDeleteAsync(
            $"/api/conversations/{conversationId}",
            callerReg.AccessToken);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Remaining participants can still access the conversation
        var messagesResponse = await _client.SendAuthorizedGetAsync(
            $"/api/conversations/{conversationId}/messages",
            other1Reg.AccessToken);
        messagesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
