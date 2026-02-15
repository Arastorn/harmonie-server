using FluentAssertions;
using Harmonie.Domain.Entities;
using Harmonie.Domain.Events;
using Harmonie.Domain.ValueObjects;
using Xunit;

namespace Harmonie.Domain.Tests;

public sealed class UserTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var email = Email.Create("test@harmonie.chat").Value!;
        var username = Username.Create("testuser").Value!;
        var passwordHash = "hashed_password";

        // Act
        var result = User.Create(email, username, passwordHash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(email);
        result.Value.Username.Should().Be(username);
        result.Value.IsActive.Should().BeTrue();
        result.Value.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldRaiseUserCreatedEvent()
    {
        // Arrange
        var email = Email.Create("test@harmonie.chat").Value!;
        var username = Username.Create("testuser").Value!;

        // Act
        var user = User.Create(email, username, "hash").Value!;

        // Assert
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserCreatedEvent>()
            .Which.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void UpdateEmail_ShouldSetEmailVerifiedToFalse()
    {
        // Arrange
        var user = User.Create(
            Email.Create("old@harmonie.chat").Value!,
            Username.Create("testuser").Value!,
            "hash").Value!;
        user.VerifyEmail();
        var newEmail = Email.Create("new@harmonie.chat").Value!;

        // Act
        user.UpdateEmail(newEmail);

        // Assert
        user.Email.Should().Be(newEmail);
        user.IsEmailVerified.Should().BeFalse();
    }
}
