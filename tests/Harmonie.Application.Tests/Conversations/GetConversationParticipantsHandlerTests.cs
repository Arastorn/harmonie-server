using FluentAssertions;
using Harmonie.Application.Common;
using Harmonie.Application.Features.Conversations.GetConversationParticipants;
using Harmonie.Application.Tests.Common;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Users;
using Harmonie.Domain.Entities.Conversations;
using Harmonie.Domain.ValueObjects.Conversations;
using Harmonie.Domain.ValueObjects.Users;
using Moq;
using Xunit;

namespace Harmonie.Application.Tests.Conversations;

public sealed class GetConversationParticipantsHandlerTests
{
    private readonly Mock<IConversationParticipantRepository> _participantRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly GetConversationParticipantsHandler _handler;

    public GetConversationParticipantsHandlerTests()
    {
        _participantRepositoryMock = new Mock<IConversationParticipantRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _participantRepositoryMock
            .Setup(x => x.GetByConversationIdAsync(It.IsAny<ConversationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _userRepositoryMock
            .Setup(x => x.GetManyByIdsAsync(It.IsAny<IReadOnlyList<UserId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new GetConversationParticipantsHandler(
            _participantRepositoryMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenCallerIsNotParticipant_ShouldReturnForbidden()
    {
        var conversationId = ConversationId.New();
        var userId = UserId.New();

        _participantRepositoryMock
            .Setup(x => x.GetAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationParticipant?)null);

        var response = await _handler.HandleAsync(conversationId, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.Conversation.AccessDenied);
    }

    [Fact]
    public async Task HandleAsync_WhenNoParticipants_ShouldReturnEmptyList()
    {
        var userId = UserId.New();
        var conversationId = ConversationId.New();
        var participant = ApplicationTestBuilders.CreateConversationParticipant(conversationId, userId);

        _participantRepositoryMock
            .Setup(x => x.GetAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);

        _participantRepositoryMock
            .Setup(x => x.GetByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([participant]);

        _userRepositoryMock
            .Setup(x => x.GetManyByIdsAsync(
                It.Is<IReadOnlyList<UserId>>(ids => ids.Count == 1 && ids[0] == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var response = await _handler.HandleAsync(conversationId, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Participants.Should().HaveCount(1);

        var dto = response.Data.Participants[0];
        dto.UserId.Should().Be(userId.Value);
        dto.Username.Should().Be("Unknown");
        dto.JoinedAtUtc.Should().Be(participant.JoinedAtUtc);
        dto.IsHidden.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenParticipantsHaveUserProfiles_ShouldReturnEnrichedData()
    {
        var userId = UserId.New();
        var otherUserId = UserId.New();
        var conversationId = ConversationId.New();

        var callerParticipant = ApplicationTestBuilders.CreateConversationParticipant(conversationId, userId);
        var otherParticipant = ApplicationTestBuilders.CreateConversationParticipant(conversationId, otherUserId);
        var otherUser = ApplicationTestBuilders.CreateUser(otherUserId);

        _participantRepositoryMock
            .Setup(x => x.GetAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(callerParticipant);

        _participantRepositoryMock
            .Setup(x => x.GetByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([callerParticipant, otherParticipant]);

        _userRepositoryMock
            .Setup(x => x.GetManyByIdsAsync(
                It.Is<IReadOnlyList<UserId>>(ids => ids.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([otherUser]);

        var response = await _handler.HandleAsync(conversationId, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeTrue();
        response.Data!.Participants.Should().HaveCount(2);

        var callerDto = response.Data.Participants[0];
        callerDto.UserId.Should().Be(userId.Value);
        callerDto.Username.Should().Be("Unknown");

        var otherDto = response.Data.Participants[1];
        otherDto.UserId.Should().Be(otherUserId.Value);
        otherDto.Username.Should().Be(otherUser.Username.Value);
        otherDto.JoinedAtUtc.Should().Be(otherParticipant.JoinedAtUtc);
        otherDto.IsHidden.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenParticipantIsHidden_ShouldReflectIsHidden()
    {
        var userId = UserId.New();
        var conversationId = ConversationId.New();
        var hiddenParticipant = ConversationParticipant.Rehydrate(
            conversationId, userId, DateTime.UtcNow.AddDays(-1), hiddenAtUtc: DateTime.UtcNow);

        _participantRepositoryMock
            .Setup(x => x.GetAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hiddenParticipant);

        _participantRepositoryMock
            .Setup(x => x.GetByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([hiddenParticipant]);

        var response = await _handler.HandleAsync(conversationId, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeTrue();
        response.Data!.Participants.Should().HaveCount(1);
        response.Data.Participants[0].IsHidden.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenNonExistentConversation_ShouldReturnForbidden()
    {
        var conversationId = ConversationId.New();
        var userId = UserId.New();

        _participantRepositoryMock
            .Setup(x => x.GetAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationParticipant?)null);

        var response = await _handler.HandleAsync(conversationId, userId, TestContext.Current.CancellationToken);

        response.Success.Should().BeFalse();
        response.Error!.Code.Should().Be(ApplicationErrorCodes.Conversation.AccessDenied);
    }
}
