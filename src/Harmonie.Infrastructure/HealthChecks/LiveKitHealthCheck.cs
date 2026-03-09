using Microsoft.Extensions.Diagnostics.HealthChecks;
using Harmonie.Infrastructure.LiveKit;
using Microsoft.Extensions.DependencyInjection;

namespace Harmonie.Infrastructure.HealthChecks;

public sealed class LiveKitHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public LiveKitHealthCheck(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var liveKitRoomApiClient = scope.ServiceProvider.GetRequiredService<ILiveKitRoomApiClient>();

            await liveKitRoomApiClient.ListRoomsAsync(cancellationToken);
            return HealthCheckResult.Healthy("LiveKit is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("LiveKit is unreachable.", exception);
        }
    }
}
