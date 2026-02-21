using FluentAssertions;
using Harmonie.Domain.ValueObjects;
using Xunit;

namespace Harmonie.Domain.Tests;

public sealed class UsernameTests
{
    [Theory]
    [InlineData("john")]
    [InlineData("john_doe")]
    [InlineData("john-doe")]
    [InlineData("john123")]
    public void Create_WithValidUsername_ShouldSucceed(string validUsername)
    {
        // Act
        var result = Username.Create(validUsername);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Theory]
    [InlineData("ab")]  // Too short
    [InlineData("a")]   // Too short
    [InlineData("_john")]  // Starts with underscore
    [InlineData("-john")]  // Starts with hyphen
    [InlineData("john_")]  // Ends with underscore
    [InlineData("john@doe")]  // Invalid character
    [InlineData("john doe")]  // Space
    public void Create_WithInvalidUsername_ShouldFail(string invalidUsername)
    {
        // Act
        var result = Username.Create(invalidUsername);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
