using System.Runtime.InteropServices.JavaScript;
using FluentAssertions;
using Harmonie.Application.Common;
using Harmonie.Application.Features.Users.UpdateUserStatus;
using Harmonie.Application.Interfaces.Guilds;
using Harmonie.Application.Interfaces.Users;
using Harmonie.Domain.Entities.Guilds;
using Harmonie.Domain.Entities.Users;
using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Harmonie.Application.Tests.Users;

public sealed class UpdateUserStatusHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IGuildMemberRepository> _guildMemberRepositoryMock;
    private readonly Mock<IUserPresenceNotifier> _userPresenceNotifierMock;
    private readonly UpdateUserStatusHandler _handler;

    public UpdateUserStatusHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _guildMemberRepositoryMock = new Mock<IGuildMemberRepository>();
        _userPresenceNotifierMock = new Mock<IUserPresenceNotifier>();
        _handler = new UpdateUserStatusHandler(
            _userRepositoryMock.Object,
            _guildMemberRepositoryMock.Object,
            _userPresenceNotifierMock.Object,
            NullLogger<UpdateUserStatusHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WithValidStatus_ShouldUpdateAndReturnStatus()
    {
        var user = CreateUser();
        var request = new UpdateUserStatusRequest("dnd");

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _guildMemberRepositoryMock
            .Setup(x => x.GetUserGuildMembershipsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserGuildMembership>());

        var response = await _handler.HandleAsync(request, user.Id);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.UserId.Should().Be(user.Id.ToString());
        response.Data.Status.Should().Be("dnd");

        _userRepositoryMock.Verify(
            x => x.UpdateStatusAsync(
                user.Id,
                "dnd",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var userId = UserId.New();
        var request = new UpdateUserStatusRequest("online");

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var response = await _handler.HandleAsync(request, userId);

        response.Success.Should().BeFalse();
        response.Data.Should().BeNull();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.User.NotFound);

        _userRepositoryMock.Verify(
            x => x.UpdateStatusAsync(
                It.IsAny<UserId>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidStatus_ShouldReturnValidationFailure()
    {
        var user = CreateUser();
        var request = new UpdateUserStatusRequest("away");

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var response = await _handler.HandleAsync(request, user.Id);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.Common.ValidationFailed);
        response.Error.Errors.Should().NotBeNull();
        response.Error.Errors!.Should().ContainKey(nameof(request.Status));

        _userRepositoryMock.Verify(
            x => x.UpdateStatusAsync(
                It.IsAny<UserId>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInvisible_ShouldBroadcastOfflineToGuilds()
    {
        var user = CreateUser();
        var request = new UpdateUserStatusRequest("invisible");
        var guildId = GuildId.New();
        var guild = CreateGuild(guildId, user.Id);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _guildMemberRepositoryMock
            .Setup(x => x.GetUserGuildMembershipsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new UserGuildMembership(guild, Domain.Enums.GuildRole.Member, DateTime.UtcNow)
            });

        var response = await _handler.HandleAsync(request, user.Id);

        response.Success.Should().BeTrue();
        response.Data!.Status.Should().Be("invisible");

        _userPresenceNotifierMock.Verify(
            x => x.NotifyStatusChangedAsync(
                It.Is<UserPresenceChangedNotification>(n =>
                    n.UserId == user.Id &&
                    n.Status == "offline" &&
                    n.GuildIds.Count == 1 &&
                    n.GuildIds[0] == guildId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithOnline_ShouldBroadcastOnlineToGuilds()
    {
        var user = CreateUser();
        var request = new UpdateUserStatusRequest("online");
        var guildId = GuildId.New();
        var guild = CreateGuild(guildId, user.Id);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _guildMemberRepositoryMock
            .Setup(x => x.GetUserGuildMembershipsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new UserGuildMembership(guild, Domain.Enums.GuildRole.Member, DateTime.UtcNow)
            });

        var response = await _handler.HandleAsync(request, user.Id);

        response.Success.Should().BeTrue();

        _userPresenceNotifierMock.Verify(
            x => x.NotifyStatusChangedAsync(
                It.Is<UserPresenceChangedNotification>(n =>
                    n.Status == "online"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasNoGuilds_ShouldNotBroadcast()
    {
        var user = CreateUser();
        var request = new UpdateUserStatusRequest("idle");

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _guildMemberRepositoryMock
            .Setup(x => x.GetUserGuildMembershipsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserGuildMembership>());

        var response = await _handler.HandleAsync(request, user.Id);

        response.Success.Should().BeTrue();

        _userPresenceNotifierMock.Verify(
            x => x.NotifyStatusChangedAsync(
                It.IsAny<UserPresenceChangedNotification>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static User CreateUser()
    {
        var emailResult = Email.Create($"test-{Guid.NewGuid():N}@harmonie.chat");
        if (emailResult.IsFailure || emailResult.Value is null)
            throw new InvalidOperationException("Failed to create email for tests.");

        var usernameResult = Username.Create($"user{Guid.NewGuid():N}"[..20]);
        if (usernameResult.IsFailure || usernameResult.Value is null)
            throw new InvalidOperationException("Failed to create username for tests.");

        var userResult = User.Create(
            emailResult.Value,
            usernameResult.Value,
            "hashed_password");
        if (userResult.IsFailure || userResult.Value is null)
            throw new InvalidOperationException("Failed to create user for tests.");

        return userResult.Value;
    }

    private static Guild CreateGuild(GuildId guildId, UserId ownerId)
    {
        var nameResult = GuildName.Create($"guild-{Guid.NewGuid():N}"[..20]);
        if (nameResult.IsFailure || nameResult.Value is null)
            throw new InvalidOperationException("Failed to create guild name for tests.");

        return Guild.Rehydrate(
            guildId,
            nameResult.Value,
            ownerId,
            createdAtUtc: DateTime.UtcNow,
            updatedAtUtc: DateTime.UtcNow);
    }
}
