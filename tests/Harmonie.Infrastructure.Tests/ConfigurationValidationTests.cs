using FluentAssertions;
using Harmonie.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Harmonie.Infrastructure.Tests;

public sealed class ConfigurationValidationTests
{
    [Fact]
    public void JwtSettings_WithShortSecret_ShouldFailValidation()
    {
        using var serviceProvider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "too-short",
            ["Jwt:Issuer"] = "harmonie",
            ["Jwt:Audience"] = "harmonie-client",
            ["LiveKit:PublicUrl"] = "ws://localhost:7880",
            ["LiveKit:InternalUrl"] = "http://localhost:7880",
            ["LiveKit:ApiKey"] = "devkey",
            ["LiveKit:ApiSecret"] = "devsecret-that-is-long-enough-for-hmac-signing",
            ["ObjectStorage:LocalBasePath"] = "uploads",
            ["ObjectStorage:LocalBaseUrl"] = "http://localhost:5000/files",
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=harmonie;Username=user;Password=password"
        });

        var act = () => serviceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void LiveKitSettings_WithInvalidUrl_ShouldFailValidation()
    {
        using var serviceProvider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "c4ed062a496bfa0e2e5f1977960bcdc1e4ec09983e6e22af468f5b45fb902678",
            ["Jwt:Issuer"] = "harmonie",
            ["Jwt:Audience"] = "harmonie-client",
            ["LiveKit:PublicUrl"] = "not-a-url",
            ["LiveKit:ApiKey"] = "devkey",
            ["LiveKit:ApiSecret"] = "devsecret-that-is-long-enough-for-hmac-signing",
            ["ObjectStorage:LocalBasePath"] = "uploads",
            ["ObjectStorage:LocalBaseUrl"] = "http://localhost:5000/files",
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=harmonie;Username=user;Password=password"
        });

        var act = () => serviceProvider.GetRequiredService<IOptions<LiveKitSettings>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void ObjectStorageSettings_WithInvalidBaseUrl_ShouldFailValidation()
    {
        using var serviceProvider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "c4ed062a496bfa0e2e5f1977960bcdc1e4ec09983e6e22af468f5b45fb902678",
            ["Jwt:Issuer"] = "harmonie",
            ["Jwt:Audience"] = "harmonie-client",
            ["LiveKit:PublicUrl"] = "ws://localhost:7880",
            ["LiveKit:InternalUrl"] = "http://localhost:7880",
            ["LiveKit:ApiKey"] = "devkey",
            ["LiveKit:ApiSecret"] = "devsecret-that-is-long-enough-for-hmac-signing",
            ["ObjectStorage:LocalBasePath"] = "uploads",
            ["ObjectStorage:LocalBaseUrl"] = "invalid-url",
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=harmonie;Username=user;Password=password"
        });

        var act = () => serviceProvider.GetRequiredService<IOptions<ObjectStorageSettings>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    private static ServiceProvider BuildServiceProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
