using Xunit;

namespace Harmonie.Infrastructure.Tests;

public sealed class LiveKitIntegrationFactAttribute : FactAttribute
{
    public LiveKitIntegrationFactAttribute()
    {
        var enabled = string.Equals(
            Environment.GetEnvironmentVariable("RUN_LIVEKIT_INTEGRATION"),
            "1",
            StringComparison.Ordinal);

        if (!enabled)
            Skip = "Set RUN_LIVEKIT_INTEGRATION=1 to run tests against a real LiveKit server.";
    }
}
