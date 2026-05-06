using FluentAssertions;
using Harmonie.Application.Common;
using Harmonie.Application.Features.Conversations.GetConversationVoiceParticipants;
using Harmonie.Application.Tests.Common;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Voice;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;
using Moq;
using Xunit;

namespace Harmonie.Application.Tests.Conversations;

public sealed class GetConversationVoiceParticipantsHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<ILiveKitRoomService> _liveKitRoomServiceMock;
    private readonly Mock<IConversationVoiceParticipantCache> _voiceParticipantCacheMock;
    private readonly GetConversationVoiceParticipantsHandler _handler;

    public GetConversationVoiceParticipantsHandlerTests()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _liveKitRoomServiceMock = new Mock<ILiveKitRoomService>();
        _voiceParticipantCacheMock = new Mock<IConversationVoiceParticipantCache>();

        _voiceParticipantCacheMock
            .Setup(x => x.GetAsync(It.IsAny<ConversationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _liveKitRoomServiceMock
            .Setup(x => x.ListConversationParticipantsAsync(It.IsAny<ConversationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new GetConversationVoiceParticipantsHandler(
            _conversationRepositoryMock.Object,
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
    public async Task HandleAsync_WhenRoomIsEmpty_ShouldReturnEmptyList()
    {
        var userId = UserId.New();
        var conversation = ApplicationTestBuilders.CreateConversation(userId, UserId.New());
        var participant = ApplicationTestBuilders.CreateConversationParticipant(conversation.Id, userId);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdWithParticipantCheckAsync(conversation.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationAccess(conversation, participant));

        var response = await _handler.HandleAsync(conversation.Id, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Participants.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenParticipantsInLiveKit_ShouldReturnEnrichedWithCacheData()
    {
        var userId = UserId.New();
        var otherUserId = UserId.New();
        var conversation = ApplicationTestBuilders.CreateConversation(userId, otherUserId);
        var participant = ApplicationTestBuilders.CreateConversationParticipant(conversation.Id, userId);
        var cached = new CachedVoiceParticipant(otherUserId, "other_user", "Other User", null, "#ABC", null, null);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdWithParticipantCheckAsync(conversation.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationAccess(conversation, participant));

        _liveKitRoomServiceMock
            .Setup(x => x.ListConversationParticipantsAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new VoiceChannelParticipant(otherUserId, "other_user", IsSharingScreen: true)]);

        _voiceParticipantCacheMock
            .Setup(x => x.GetAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([cached]);

        var response = await _handler.HandleAsync(conversation.Id, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeTrue();
        response.Data!.Participants.Should().HaveCount(1);

        var dto = response.Data.Participants[0];
        dto.UserId.Should().Be(otherUserId.Value);
        dto.DisplayName.Should().Be("Other User");
        dto.AvatarColor.Should().Be("#ABC");
        dto.IsSharingScreen.Should().BeTrue();
    }
}
