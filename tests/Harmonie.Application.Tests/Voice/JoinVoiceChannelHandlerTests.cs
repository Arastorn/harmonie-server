using FluentAssertions;
using Harmonie.Application.Common;
using Harmonie.Application.Features.Channels.JoinVoiceChannel;
using Harmonie.Application.Tests.Common;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Guilds;
using Harmonie.Application.Interfaces.Users;
using Harmonie.Application.Interfaces.Voice;
using Harmonie.Domain.Entities.Guilds;
using Harmonie.Domain.Entities.Users;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Users;
using Moq;
using Xunit;

namespace Harmonie.Application.Tests.Voice;

public sealed class JoinVoiceChannelHandlerTests
{
    private readonly Mock<IGuildChannelRepository> _guildChannelRepositoryMock;
    private readonly Mock<IGuildMemberRepository> _guildMemberRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILiveKitTokenService> _liveKitTokenServiceMock;
    private readonly Mock<ILiveKitRoomService> _liveKitRoomServiceMock;
    private readonly Mock<IVoiceParticipantCache> _voiceParticipantCacheMock;
    private readonly JoinVoiceChannelHandler _handler;

    public JoinVoiceChannelHandlerTests()
    {
        _guildChannelRepositoryMock = new Mock<IGuildChannelRepository>();
        _guildMemberRepositoryMock = new Mock<IGuildMemberRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _liveKitTokenServiceMock = new Mock<ILiveKitTokenService>();
        _liveKitRoomServiceMock = new Mock<ILiveKitRoomService>();
        _voiceParticipantCacheMock = new Mock<IVoiceParticipantCache>();

        _voiceParticipantCacheMock
            .Setup(x => x.GetAsync(It.IsAny<GuildChannelId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _voiceParticipantCacheMock
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<GuildChannelId>(), It.IsAny<CachedVoiceParticipant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _liveKitRoomServiceMock
            .Setup(x => x.ListChannelParticipantsAsync(It.IsAny<GuildChannelId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new JoinVoiceChannelHandler(
            _guildChannelRepositoryMock.Object,
            _guildMemberRepositoryMock.Object,
            _userRepositoryMock.Object,
            _liveKitTokenServiceMock.Object,
            _liveKitRoomServiceMock.Object,
            _voiceParticipantCacheMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenChannelDoesNotExist_ShouldReturnNotFound()
    {
        var channelId = GuildChannelId.New();
        var userId = UserId.New();

        _guildChannelRepositoryMock
            .Setup(x => x.GetByIdAsync(channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuildChannel?)null);

        var response = await _handler.HandleAsync(channelId, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.Channel.NotFound);
    }

    [Fact]
    public async Task HandleAsync_WhenChannelIsText_ShouldReturnNotVoice()
    {
        var channel = ApplicationTestBuilders.CreateChannel(GuildChannelType.Text);
        var userId = UserId.New();

        _guildChannelRepositoryMock
            .Setup(x => x.GetByIdAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        var response = await _handler.HandleAsync(channel.Id, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.Channel.NotVoice);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsNotGuildMember_ShouldReturnAccessDenied()
    {
        var channel = ApplicationTestBuilders.CreateChannel(GuildChannelType.Voice);
        var userId = UserId.New();

        _guildChannelRepositoryMock
            .Setup(x => x.GetByIdAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        _guildMemberRepositoryMock
            .Setup(x => x.IsMemberAsync(channel.GuildId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = await _handler.HandleAsync(channel.Id, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.Channel.AccessDenied);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ShouldReturnUserNotFound()
    {
        var channel = ApplicationTestBuilders.CreateChannel(GuildChannelType.Voice);
        var userId = UserId.New();

        _guildChannelRepositoryMock
            .Setup(x => x.GetByIdAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        _guildMemberRepositoryMock
            .Setup(x => x.IsMemberAsync(channel.GuildId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var response = await _handler.HandleAsync(channel.Id, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.User.NotFound);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomIsEmpty_ShouldReturnEmptyCurrentParticipants()
    {
        var channel = ApplicationTestBuilders.CreateChannel(GuildChannelType.Voice);
        var user = ApplicationTestBuilders.CreateUser();
        var roomToken = new LiveKitRoomToken(
            Token: "eyJ.token",
            Url: "ws://localhost:7880",
            RoomName: $"channel:{channel.Id}");

        _guildChannelRepositoryMock
            .Setup(x => x.GetByIdAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        _guildMemberRepositoryMock
            .Setup(x => x.IsMemberAsync(channel.GuildId, user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _liveKitTokenServiceMock
            .Setup(x => x.GenerateRoomTokenAsync(channel.Id, user.Id, user.Username.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roomToken);

        var response = await _handler.HandleAsync(channel.Id, user.Id, TestContext.Current.CancellationToken);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Token.Should().Be(roomToken.Token);
        response.Data.Url.Should().Be(roomToken.Url);
        response.Data.RoomName.Should().Be(roomToken.RoomName);
        response.Data.CurrentParticipants.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenParticipantsAreAlreadyCached_ShouldReturnCachedParticipants()
    {
        var channel = ApplicationTestBuilders.CreateChannel(GuildChannelType.Voice);
        var user = ApplicationTestBuilders.CreateUser();
        var roomToken = new LiveKitRoomToken("eyJ.token", "ws://localhost:7880", $"channel:{channel.Id}");

        var participantUserId = UserId.New();
        var lkParticipant = new VoiceChannelParticipant(participantUserId, "lk-username");
        var cachedParticipant = new CachedVoiceParticipant(
            UserId: participantUserId,
            Username: "cached-username",
            DisplayName: "Cached Display",
            AvatarFileId: null,
            AvatarColor: "#abc",
            AvatarIcon: "star",
            AvatarBg: "#fff");

        _guildChannelRepositoryMock
            .Setup(x => x.GetByIdAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        _guildMemberRepositoryMock
            .Setup(x => x.IsMemberAsync(channel.GuildId, user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _liveKitTokenServiceMock
            .Setup(x => x.GenerateRoomTokenAsync(channel.Id, user.Id, user.Username.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roomToken);

        _liveKitRoomServiceMock
            .Setup(x => x.ListChannelParticipantsAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([lkParticipant]);

        _voiceParticipantCacheMock
            .Setup(x => x.GetAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([cachedParticipant]);

        var response = await _handler.HandleAsync(channel.Id, user.Id, TestContext.Current.CancellationToken);

        response.Success.Should().BeTrue();
        response.Data!.CurrentParticipants.Should().HaveCount(1);

        var p = response.Data.CurrentParticipants[0];
        p.UserId.Should().Be(participantUserId.Value);
        p.Username.Should().Be("cached-username");
        p.DisplayName.Should().Be("Cached Display");
        p.AvatarColor.Should().Be("#abc");
        p.AvatarIcon.Should().Be("star");
        p.AvatarBg.Should().Be("#fff");

        _userRepositoryMock.Verify(
            x => x.GetManyByIdsAsync(It.IsAny<IReadOnlyList<UserId>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenParticipantNotInCache_ShouldFetchFromDbAndUpdateCache()
    {
        var channel = ApplicationTestBuilders.CreateChannel(GuildChannelType.Voice);
        var user = ApplicationTestBuilders.CreateUser();
        var roomToken = new LiveKitRoomToken("eyJ.token", "ws://localhost:7880", $"channel:{channel.Id}");

        var participantUserId = UserId.New();
        var dbUser = ApplicationTestBuilders.CreateUser();
        var lkParticipant = new VoiceChannelParticipant(dbUser.Id, dbUser.Username.Value);

        _guildChannelRepositoryMock
            .Setup(x => x.GetByIdAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        _guildMemberRepositoryMock
            .Setup(x => x.IsMemberAsync(channel.GuildId, user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _liveKitTokenServiceMock
            .Setup(x => x.GenerateRoomTokenAsync(channel.Id, user.Id, user.Username.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roomToken);

        _liveKitRoomServiceMock
            .Setup(x => x.ListChannelParticipantsAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([lkParticipant]);

        _userRepositoryMock
            .Setup(x => x.GetManyByIdsAsync(
                It.Is<IReadOnlyList<UserId>>(ids => ids.Contains(dbUser.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([dbUser]);

        var response = await _handler.HandleAsync(channel.Id, user.Id, TestContext.Current.CancellationToken);

        response.Success.Should().BeTrue();
        response.Data!.CurrentParticipants.Should().HaveCount(1);

        var p = response.Data.CurrentParticipants[0];
        p.UserId.Should().Be(dbUser.Id.Value);
        p.Username.Should().Be(dbUser.Username.Value);

        _voiceParticipantCacheMock.Verify(
            x => x.AddOrUpdateAsync(
                channel.Id,
                It.Is<CachedVoiceParticipant>(cp => cp.UserId == dbUser.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenCachedParticipantNotInLiveKit_ShouldRemoveFromCache()
    {
        var channel = ApplicationTestBuilders.CreateChannel(GuildChannelType.Voice);
        var user = ApplicationTestBuilders.CreateUser();
        var roomToken = new LiveKitRoomToken("eyJ.token", "ws://localhost:7880", $"channel:{channel.Id}");

        var staleUserId = UserId.New();
        var staleParticipant = new CachedVoiceParticipant(
            UserId: staleUserId,
            Username: "stale",
            DisplayName: null,
            AvatarFileId: null,
            AvatarColor: null,
            AvatarIcon: null,
            AvatarBg: null);

        _guildChannelRepositoryMock
            .Setup(x => x.GetByIdAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        _guildMemberRepositoryMock
            .Setup(x => x.IsMemberAsync(channel.GuildId, user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _liveKitTokenServiceMock
            .Setup(x => x.GenerateRoomTokenAsync(channel.Id, user.Id, user.Username.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roomToken);

        _voiceParticipantCacheMock
            .Setup(x => x.GetAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([staleParticipant]);

        var response = await _handler.HandleAsync(channel.Id, user.Id, TestContext.Current.CancellationToken);

        response.Success.Should().BeTrue();
        response.Data!.CurrentParticipants.Should().BeEmpty();

        _voiceParticipantCacheMock.Verify(
            x => x.RemoveAsync(channel.Id, staleUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
