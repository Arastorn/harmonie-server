using FluentAssertions;
using Harmonie.Infrastructure.Configuration;
using Harmonie.Infrastructure.HealthChecks;
using Harmonie.Infrastructure.LiveKit;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Harmonie.Infrastructure.Tests;

public sealed class HealthChecksTests
{
    [Fact]
    public async Task PostgresHealthCheck_WithInvalidConnectionString_ShouldReturnUnhealthy()
    {
        var healthCheck = new PostgresHealthCheck(Options.Create(new DatabaseSettings
        {
            ConnectionString = "Host=127.0.0.1;Port=1;Database=harmonie;Username=test;Password=test"
        }));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task LiveKitHealthCheck_WhenLiveKitIsReachable_ShouldReturnHealthy()
    {
        var roomApiClientMock = new Mock<ILiveKitRoomApiClient>();
        roomApiClientMock
            .Setup(client => client.ListRoomsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Room { Name = "room-1" }]);

        using var serviceProvider = new ServiceCollection()
            .AddScoped(_ => roomApiClientMock.Object)
            .BuildServiceProvider();

        var healthCheck = new LiveKitHealthCheck(
            serviceProvider.GetRequiredService<IServiceScopeFactory>());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task LiveKitHealthCheck_WhenLiveKitThrows_ShouldReturnUnhealthy()
    {
        var roomApiClientMock = new Mock<ILiveKitRoomApiClient>();
        roomApiClientMock
            .Setup(client => client.ListRoomsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("LiveKit unavailable"));

        using var serviceProvider = new ServiceCollection()
            .AddScoped(_ => roomApiClientMock.Object)
            .BuildServiceProvider();

        var healthCheck = new LiveKitHealthCheck(
            serviceProvider.GetRequiredService<IServiceScopeFactory>());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
