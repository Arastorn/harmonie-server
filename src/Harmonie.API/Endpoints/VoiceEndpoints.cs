using Harmonie.Application.Features.Voice.HandleLiveKitWebhook;

namespace Harmonie.API.Endpoints;

public static class VoiceEndpoints
{
    public static void MapVoiceEndpoints(this IEndpointRouteBuilder app)
    {
        HandleLiveKitWebhookEndpoint.Map(app);
    }
}
