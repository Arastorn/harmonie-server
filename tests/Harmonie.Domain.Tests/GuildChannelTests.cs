using FluentAssertions;
using Harmonie.Domain.Entities.Guilds;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Guilds;
using Xunit;

namespace Harmonie.Domain.Tests;

public sealed class GuildChannelTests
{
    [Fact]
    public void UpdateName_ShouldMarkChannelAsUpdated()
    {
        var channel = CreateChannel();

        var result = channel.UpdateName("updated-name");

        result.IsSuccess.Should().BeTrue();
        channel.Name.Should().Be("updated-name");
        channel.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void UpdatePosition_ShouldMarkChannelAsUpdated()
    {
        var channel = CreateChannel();

        var result = channel.UpdatePosition(4);

        result.IsSuccess.Should().BeTrue();
        channel.Position.Should().Be(4);
        channel.UpdatedAtUtc.Should().NotBeNull();
    }

    private static GuildChannel CreateChannel()
    {
        var channelResult = GuildChannel.Create(
            GuildId.New(),
            "general",
            GuildChannelType.Text,
            isDefault: true,
            position: 0);

        if (channelResult.IsFailure || channelResult.Value is null)
            throw new InvalidOperationException("Failed to create guild channel for tests.");

        return channelResult.Value;
    }
}
