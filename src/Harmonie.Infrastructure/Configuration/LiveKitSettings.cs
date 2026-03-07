namespace Harmonie.Infrastructure.Configuration;

public sealed class LiveKitSettings
{
    public string Url { get; init; } = string.Empty;
    public string ServerUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;

    public string GetServerUrl()
        => string.IsNullOrWhiteSpace(ServerUrl)
            ? Url
            : ServerUrl;
}
