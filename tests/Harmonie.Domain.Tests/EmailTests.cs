using FluentAssertions;
using Harmonie.Domain.ValueObjects;
using Xunit;

namespace Harmonie.Domain.Tests;

public sealed class EmailTests
{
    [Theory]
    [InlineData("test@harmonie.chat")]
    [InlineData("user.name+tag@example.com")]
    [InlineData("x@y.z")]
    public void Create_WithValidEmail_ShouldSucceed(string validEmail)
    {
        // Act
        var result = Email.Create(validEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be(validEmail.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Create_WithInvalidEmail_ShouldFail(string invalidEmail)
    {
        // Act
        var result = Email.Create(invalidEmail);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
