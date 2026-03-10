using FluentAssertions;
using Harmonie.Application.Common;
using Harmonie.Application.Features.Guilds.UpdateGuild;
using Harmonie.Application.Interfaces;
using Harmonie.Domain.Entities;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Harmonie.Application.Tests;

public sealed class UpdateGuildHandlerTests
{
    private readonly Mock<IGuildRepository> _guildRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUnitOfWorkTransaction> _transactionMock;
    private readonly UpdateGuildHandler _handler;

    public UpdateGuildHandlerTests()
    {
        _guildRepositoryMock = new Mock<IGuildRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _transactionMock = new Mock<IUnitOfWorkTransaction>();

        _unitOfWorkMock
            .Setup(x => x.BeginAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _transactionMock
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _transactionMock
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        _handler = new UpdateGuildHandler(
            _guildRepositoryMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<UpdateGuildHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenGuildDoesNotExist_ShouldReturnNotFound()
    {
        var guildId = GuildId.New();
        var callerId = UserId.New();

        _guildRepositoryMock
            .Setup(x => x.GetWithCallerRoleAsync(guildId, callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuildAccessContext?)null);

        var response = await _handler.HandleAsync(guildId, callerId, new UpdateGuildRequest());

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.Guild.NotFound);
    }

    [Fact]
    public async Task HandleAsync_WhenCallerIsMemberNotAdminNorOwner_ShouldReturnAccessDenied()
    {
        var guild = CreateGuild();
        var callerId = UserId.New();

        _guildRepositoryMock
            .Setup(x => x.GetWithCallerRoleAsync(guild.Id, callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildAccessContext(guild, GuildRole.Member));

        var response = await _handler.HandleAsync(guild.Id, callerId, new UpdateGuildRequest());

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.Guild.AccessDenied);
    }

    [Fact]
    public async Task HandleAsync_WhenAdminUpdatesGuild_ShouldPersistAndReturnIcon()
    {
        var guild = CreateGuild();
        var adminId = UserId.New();

        _guildRepositoryMock
            .Setup(x => x.GetWithCallerRoleAsync(guild.Id, adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildAccessContext(guild, GuildRole.Admin));

        var request = new UpdateGuildRequest
        {
            Name = "Updated Guild",
            NameIsSet = true,
            IconUrl = "https://cdn.harmonie.chat/guild-updated.png",
            IconUrlIsSet = true,
            IconIsSet = true,
            IconColor = "#7C3AED",
            IconColorIsSet = true,
            IconName = "sword",
            IconNameIsSet = true,
            IconBg = "#1F2937",
            IconBgIsSet = true
        };

        var response = await _handler.HandleAsync(guild.Id, adminId, request);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be("Updated Guild");
        response.Data.IconUrl.Should().Be("https://cdn.harmonie.chat/guild-updated.png");
        response.Data.Icon.Should().NotBeNull();
        response.Data.Icon!.Name.Should().Be("sword");

        _guildRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<Guild>(updatedGuild =>
                    updatedGuild.Name.Value == "Updated Guild"
                    && updatedGuild.IconUrl == "https://cdn.harmonie.chat/guild-updated.png"
                    && updatedGuild.IconColor == "#7C3AED"
                    && updatedGuild.IconName == "sword"
                    && updatedGuild.IconBg == "#1F2937"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenIconIsCleared_ShouldSetIconPayloadToNull()
    {
        var guild = CreateGuild();
        guild.UpdateIconColor("#INITIAL");
        guild.UpdateIconName("shield");
        guild.UpdateIconBg("#000000");

        var ownerId = guild.OwnerUserId;

        _guildRepositoryMock
            .Setup(x => x.GetWithCallerRoleAsync(guild.Id, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildAccessContext(guild, GuildRole.Member));

        var request = new UpdateGuildRequest
        {
            IconIsSet = true,
            IconColor = null,
            IconColorIsSet = true,
            IconName = null,
            IconNameIsSet = true,
            IconBg = null,
            IconBgIsSet = true
        };

        var response = await _handler.HandleAsync(guild.Id, ownerId, request);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Icon.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenNoFieldsSet_ShouldNotPersist()
    {
        var guild = CreateGuild();
        var ownerId = guild.OwnerUserId;

        _guildRepositoryMock
            .Setup(x => x.GetWithCallerRoleAsync(guild.Id, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildAccessContext(guild, GuildRole.Member));

        var response = await _handler.HandleAsync(guild.Id, ownerId, new UpdateGuildRequest());

        response.Success.Should().BeTrue();
        _guildRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Guild>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.BeginAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Guild CreateGuild()
    {
        var guildNameResult = GuildName.Create("Guild Alpha");
        if (guildNameResult.IsFailure || guildNameResult.Value is null)
            throw new InvalidOperationException("Failed to create guild name for tests.");

        return Guild.Rehydrate(
            GuildId.New(),
            guildNameResult.Value,
            UserId.New(),
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddDays(-1));
    }
}
