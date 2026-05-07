using FluentAssertions;
using Harmonie.Application.Common;
using Harmonie.Application.Features.Conversations.JoinConversationVoice;
using Harmonie.Application.Tests.Common;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Users;
using Harmonie.Application.Interfaces.Voice;
using Harmonie.Domain.Entities.Conversations;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;
using Moq;
using Xunit;

namespace Harmonie.Application.Tests.Conversations;

public sealed class JoinConversationVoiceHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILiveKitTokenService> _liveKitTokenServiceMock;
    private readonly Mock<ILiveKitRoomService> _liveKitRoomServiceMock;
    private readonly Mock<IConversationVoiceParticipantCache> _voiceParticipantCacheMock;
    private readonly JoinConversationVoiceHandler _handler;

    public JoinConversationVoiceHandlerTests()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _liveKitTokenServiceMock = new Mock<ILiveKitTokenService>();
        _liveKitRoomServiceMock = new Mock<ILiveKitRoomService>();
        _voiceParticipantCacheMock = new Mock<IConversationVoiceParticipantCache>();

        _voiceParticipantCacheMock
            .Setup(x => x.GetAsync(It.IsAny<ConversationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _voiceParticipantCacheMock
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<ConversationId>(), It.IsAny<CachedVoiceParticipant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _liveKitRoomServiceMock
            .Setup(x => x.ListConversationParticipantsAsync(It.IsAny<ConversationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new JoinConversationVoiceHandler(
            _conversationRepositoryMock.Object,
            _userRepositoryMock.Object,
            _liveKitTokenServiceMock.Object,
            _liveKitRoomServiceMock.Object,
            _voiceParticipantCacheMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenConversationDoesNotExist_ShouldReturnNotFound()
    {
        var conversationId = ConversationId.New();
        var userId = UserId.New();

        _conversationRepositoryMock
            .Setup(x => x.GetByIdWithParticipantCheckAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationAccess?)null);

        var response = await _handler.HandleAsync(conversationId, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.Conversation.NotFound);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsNotParticipant_ShouldReturnVoiceAccessDenied()
    {
        var conversationId = ConversationId.New();
        var userId = UserId.New();
        var conversation = ApplicationTestBuilders.CreateConversation(userId, UserId.New());

        _conversationRepositoryMock
            .Setup(x => x.GetByIdWithParticipantCheckAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationAccess(conversation, Participant: null));

        var response = await _handler.HandleAsync(conversationId, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.Conversation.VoiceAccessDenied);
    }

    [Fact]
    public async Task HandleAsync_WhenUserProfileNotFound_ShouldReturnUserNotFound()
    {
        var userId = UserId.New();
        var conversation = ApplicationTestBuilders.CreateConversation(userId, UserId.New());
        var participant = ApplicationTestBuilders.CreateConversationParticipant(conversation.Id, userId);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdWithParticipantCheckAsync(conversation.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationAccess(conversation, participant));

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Harmonie.Domain.Entities.Users.User?)null);

        var response = await _handler.HandleAsync(conversation.Id, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.User.NotFound);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomIsEmpty_ShouldReturnTokenWithNoParticipants()
    {
        var userId = UserId.New();
        var user = ApplicationTestBuilders.CreateUser(userId);
        var conversation = ApplicationTestBuilders.CreateConversation(userId, UserId.New());
        var participant = ApplicationTestBuilders.CreateConversationParticipant(conversation.Id, userId);
        var roomName = $"conversation:{conversation.Id}";

        _conversationRepositoryMock
            .Setup(x => x.GetByIdWithParticipantCheckAsync(conversation.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationAccess(conversation, participant));

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _liveKitTokenServiceMock
            .Setup(x => x.GenerateConversationRoomTokenAsync(conversation.Id, userId, user.Username.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveKitRoomToken("jwt-token", "ws://livekit", roomName));

        var response = await _handler.HandleAsync(conversation.Id, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Token.Should().Be("jwt-token");
        response.Data.RoomName.Should().Be(roomName);
        response.Data.CurrentParticipants.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenParticipantInLiveKitButNotInCache_ShouldFetchFromDbAndCacheIt()
    {
        var userId = UserId.New();
        var user = ApplicationTestBuilders.CreateUser(userId);
        var otherUser = ApplicationTestBuilders.CreateUser();
        var conversation = ApplicationTestBuilders.CreateConversation(userId, otherUser.Id);
        var participant = ApplicationTestBuilders.CreateConversationParticipant(conversation.Id, userId);
        var roomName = $"conversation:{conversation.Id}";

        _conversationRepositoryMock
            .Setup(x => x.GetByIdWithParticipantCheckAsync(conversation.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationAccess(conversation, participant));

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _liveKitTokenServiceMock
            .Setup(x => x.GenerateConversationRoomTokenAsync(conversation.Id, userId, user.Username.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveKitRoomToken("jwt-token", "ws://livekit", roomName));

        _liveKitRoomServiceMock
            .Setup(x => x.ListConversationParticipantsAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new VoiceChannelParticipant(otherUser.Id, otherUser.Username.Value)]);

        _userRepositoryMock
            .Setup(x => x.GetManyByIdsAsync(It.IsAny<IReadOnlyList<UserId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([otherUser]);

        var response = await _handler.HandleAsync(conversation.Id, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeTrue();
        response.Data!.CurrentParticipants.Should().HaveCount(1);
        response.Data.CurrentParticipants[0].UserId.Should().Be(otherUser.Id.Value);

        _voiceParticipantCacheMock.Verify(
            x => x.AddOrUpdateAsync(conversation.Id, It.IsAny<CachedVoiceParticipant>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenStaleParticipantInCache_ShouldRemoveFromCache()
    {
        var userId = UserId.New();
        var user = ApplicationTestBuilders.CreateUser(userId);
        var staleUserId = UserId.New();
        var conversation = ApplicationTestBuilders.CreateConversation(userId, UserId.New());
        var participant = ApplicationTestBuilders.CreateConversationParticipant(conversation.Id, userId);
        var staleParticipant = new CachedVoiceParticipant(staleUserId, "stale", null, null, null, null, null);
        var roomName = $"conversation:{conversation.Id}";

        _conversationRepositoryMock
            .Setup(x => x.GetByIdWithParticipantCheckAsync(conversation.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationAccess(conversation, participant));

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _liveKitTokenServiceMock
            .Setup(x => x.GenerateConversationRoomTokenAsync(conversation.Id, userId, user.Username.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveKitRoomToken("jwt-token", "ws://livekit", roomName));

        _voiceParticipantCacheMock
            .Setup(x => x.GetAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([staleParticipant]);

        var response = await _handler.HandleAsync(conversation.Id, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeTrue();

        _voiceParticipantCacheMock.Verify(
            x => x.RemoveAsync(conversation.Id, staleUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
