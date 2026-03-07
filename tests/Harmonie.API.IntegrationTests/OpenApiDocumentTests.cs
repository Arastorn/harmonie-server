using System.Net;
using System.Text.Json.Nodes;
using FluentAssertions;
using Harmonie.Application.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Harmonie.API.IntegrationTests;

public sealed class OpenApiDocumentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OpenApiDocumentTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OpenApiDocument_ShouldDescribeProblemDetailsExamplesForSecuredValidatedEndpoints()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        document.Should().NotBeNull();

        var getGuildChannels = document!["paths"]?["/api/guilds/{guildId}/channels"]?["get"]?["responses"];
        getGuildChannels.Should().NotBeNull();

        var validationExample = getGuildChannels!["400"]?["content"]?["application/json"]?["examples"]?[ApplicationErrorCodes.Common.ValidationFailed]?["value"];
        validationExample.Should().NotBeNull();
        validationExample!["code"]?.GetValue<string>().Should().Be(ApplicationErrorCodes.Common.ValidationFailed);
        validationExample["status"]?.GetValue<int>().Should().Be(400);
        validationExample["traceId"]?.GetValue<string>().Should().Be("trace-id");
        validationExample["errors"]?["field"]?[0]?["code"]?.GetValue<string>().Should().Be(ApplicationErrorCodes.Validation.Required);
        validationExample["errors"]?["field"]?[0]?["detail"]?.GetValue<string>().Should().Be("Field is required");
        validationExample["detail"]?.GetValue<string>().Should().Be("Request validation failed");

        var unauthorizedExample = getGuildChannels["401"]?["content"]?["application/json"]?["examples"]?[ApplicationErrorCodes.Auth.InvalidCredentials]?["value"];
        unauthorizedExample.Should().NotBeNull();
        unauthorizedExample!["code"]?.GetValue<string>().Should().Be(ApplicationErrorCodes.Auth.InvalidCredentials);
        unauthorizedExample["status"]?.GetValue<int>().Should().Be(401);
        unauthorizedExample["traceId"]?.GetValue<string>().Should().Be("trace-id");
        unauthorizedExample["detail"]?.GetValue<string>().Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task OpenApiDocument_ShouldListAllRegisterConflictCodesInResponseDescription()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        document.Should().NotBeNull();

        var registerConflict = document!["paths"]?["/api/auth/register"]?["post"]?["responses"]?["409"];
        registerConflict.Should().NotBeNull();

        var description = registerConflict!["description"]?.GetValue<string>();
        description.Should().NotBeNull();
        description.Should().Contain(ApplicationErrorCodes.Auth.DuplicateEmail);
        description.Should().Contain(ApplicationErrorCodes.Auth.DuplicateUsername);

        var conflictExamples = registerConflict["content"]?["application/json"]?["examples"];
        conflictExamples?[ApplicationErrorCodes.Auth.DuplicateEmail]?["value"]?["code"]?.GetValue<string>()
            .Should().Be(ApplicationErrorCodes.Auth.DuplicateEmail);
        conflictExamples?[ApplicationErrorCodes.Auth.DuplicateUsername]?["value"]?["code"]?.GetValue<string>()
            .Should().Be(ApplicationErrorCodes.Auth.DuplicateUsername);
    }
}
