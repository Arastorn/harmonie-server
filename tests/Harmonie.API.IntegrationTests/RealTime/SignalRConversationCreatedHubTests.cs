using FluentAssertions;
using Harmonie.API.IntegrationTests.Common;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Harmonie.API.IntegrationTests;

public sealed class SignalRConversationCreatedHubTests : IClassFixture<HarmonieWebApplicationFactory>
{
    private readonly HarmonieWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SignalRConversationCreatedHubTests(HarmonieWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ConversationCreated_WhenParticipantConnected_ShouldReceiveEvent()
    {
        var creator = await AuthTestHelper.RegisterAsync(_client);
        var participant = await AuthTestHelper.RegisterAsync(_client);

        await using var connection = CreateHubConnection(participant.AccessToken);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var eventReceived = new TaskCompletionSource<SignalRConversationCreatedEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        connection.On("Ready", () => ready.TrySetResult());
        connection.On<SignalRConversationCreatedEvent>("ConversationCreated", payload =>
        {
            eventReceived.TrySetResult(payload);
        });

        await connection.StartAsync(TestContext.Current.CancellationToken);
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        var group = await ConversationTestHelper.CreateGroupConversationAsync(
            _client, creator.AccessToken, "Integration Test Group",
            [creator.UserId, participant.UserId]);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var completedTask = await Task.WhenAny(eventReceived.Task, Task.Delay(Timeout.InfiniteTimeSpan, timeout.Token));
        completedTask.Should().Be(eventReceived.Task);

        var eventPayload = await eventReceived.Task;
        eventPayload.ConversationId.Should().Be(group.ConversationId.ToString());
        eventPayload.Name.Should().Be("Integration Test Group");
        eventPayload.Participants.Should().Contain(p => p.UserId == creator.UserId);
        eventPayload.Participants.Should().Contain(p => p.UserId == participant.UserId);
    }

    private HubConnection CreateHubConnection(string accessToken)
    {
        var baseAddress = _client.BaseAddress ?? new Uri("http://localhost");
        var hubUri = new Uri(baseAddress, "/hubs/realtime");

        return new HubConnectionBuilder()
            .WithUrl(hubUri, options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();
    }

    private sealed record SignalRConversationCreatedEvent(
        string ConversationId,
        string? Name,
        IReadOnlyList<SignalRConversationParticipantDto> Participants);

    private sealed record SignalRConversationParticipantDto(Guid UserId, string Username);
}
