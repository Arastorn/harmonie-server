namespace Harmonie.API.Configuration;

public sealed class CorsSettings
{
    public string[] AllowedOrigins { get; init; } = [];
}
