using FluentAssertions;
using Harmonie.Domain.Entities.Conversations;
using Harmonie.Domain.ValueObjects.Users;
using Xunit;

namespace Harmonie.Domain.Tests;

public sealed class ConversationTests
{
    [Fact]
    public void Create_WithDistinctUsers_ShouldSucceedAndNormalizeOrdering()
    {
        var largerUserId = UserId.From(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"));
        var smallerUserId = UserId.From(Guid.Parse("00000000-0000-0000-0000-000000000001"));

        var result = Conversation.Create(largerUserId, smallerUserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.User1Id.Should().Be(smallerUserId);
        result.Value.User2Id.Should().Be(largerUserId);
    }

    [Fact]
    public void Create_WithSameUserTwice_ShouldFail()
    {
        var userId = UserId.New();

        var result = Conversation.Create(userId, userId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }
}
