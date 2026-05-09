using FluentAssertions;
using Harmonie.Domain.ValueObjects.Common;
using Xunit;

namespace Harmonie.Domain.Tests;

public sealed class AppearanceTests
{
    [Fact]
    public void Create_WithValidValues_ShouldSucceed()
    {
        var result = Appearance.Create("#FFF", "star", "#000");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Color.Should().Be("#FFF");
        result.Value.Glyph.Should().Be("star");
        result.Value.Bg.Should().Be("#000");
        result.Value.HasValue.Should().BeTrue();
    }

    [Fact]
    public void Create_WithColorTooLong_ShouldFail()
    {
        var result = Appearance.Create(new string('c', 51), null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Appearance color is too long");
    }

    [Fact]
    public void Create_WithGlyphTooLong_ShouldFail()
    {
        var result = Appearance.Create(null, new string('g', 51), null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Appearance glyph is too long");
    }

    [Fact]
    public void Create_WithBgTooLong_ShouldFail()
    {
        var result = Appearance.Create(null, null, new string('b', 51));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Appearance background is too long");
    }

    [Fact]
    public void Create_WithAllNull_ShouldReturnEmpty()
    {
        var result = Appearance.Create(null, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Appearance.Empty);
        result.Value!.HasValue.Should().BeFalse();
    }

    [Fact]
    public void Empty_ShouldBeSameInstance()
    {
        var result = Appearance.Create(null, null, null);

        result.Value.Should().BeSameAs(Appearance.Empty);
    }

    [Fact]
    public void Create_PartialNullFields_ShouldHaveHasValueTrue()
    {
        var result = Appearance.Create("#FFF", null, null);

        result.Value!.HasValue.Should().BeTrue();
    }

    [Theory]
    [InlineData("#FFF", "star", "#000")]
    public void RecordEquality_SameValues_ShouldBeEqual(string color, string glyph, string bg)
    {
        var a = Appearance.Create(color, glyph, bg).Value!;
        var b = Appearance.Create(color, glyph, bg).Value!;

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_DifferentValues_ShouldNotBeEqual()
    {
        var a = Appearance.Create("#FFF", "star", "#000").Value!;
        var b = Appearance.Create("#000", "moon", "#FFF").Value!;

        a.Should().NotBe(b);
        (a == b).Should().BeFalse();
    }

    [Fact]
    public void HasValue_WhenAllNull_ShouldBeFalse()
    {
        var appearance = Appearance.Create(null, null, null).Value!;

        appearance.HasValue.Should().BeFalse();
    }

    [Fact]
    public void HasValue_WhenOnlyColorSet_ShouldBeTrue()
    {
        var appearance = Appearance.Create("#FFF", null, null).Value!;

        appearance.HasValue.Should().BeTrue();
    }

    [Fact]
    public void HasValue_WhenOnlyGlyphSet_ShouldBeTrue()
    {
        var appearance = Appearance.Create(null, "star", null).Value!;

        appearance.HasValue.Should().BeTrue();
    }

    [Fact]
    public void HasValue_WhenOnlyBgSet_ShouldBeTrue()
    {
        var appearance = Appearance.Create(null, null, "#000").Value!;

        appearance.HasValue.Should().BeTrue();
    }
}
