using Harmonie.Domain.Common;

namespace Harmonie.Domain.ValueObjects.Common;

/// <summary>
/// Shared visual appearance value object used by both User avatars and Guild icons.
/// Consolidates the (Color, Glyph, Bg) triplet with a single validation path.
/// </summary>
public sealed record Appearance
{
    public string? Color { get; }
    public string? Glyph { get; }
    public string? Bg { get; }

    public static readonly Appearance Empty = new(null, null, null);

    private Appearance(string? color, string? glyph, string? bg)
    {
        Color = color;
        Glyph = glyph;
        Bg = bg;
    }

    /// <summary>
    /// Create an Appearance from three optional strings.
    /// Each non-null field is validated for max length (50 chars).
    /// Returns <see cref="Empty"/> when all three fields are null.
    /// </summary>
    public static Result<Appearance> Create(string? color, string? glyph, string? bg)
    {
        if (color?.Length > 50)
            return Result.Failure<Appearance>("Appearance color is too long");
        if (glyph?.Length > 50)
            return Result.Failure<Appearance>("Appearance glyph is too long");
        if (bg?.Length > 50)
            return Result.Failure<Appearance>("Appearance background is too long");

        if (color is null && glyph is null && bg is null)
            return Result.Success(Empty);

        return Result.Success(new Appearance(color, glyph, bg));
    }

    /// <summary>
    /// Whether this appearance has any visible element set (non-null color, glyph, or bg).
    /// </summary>
    public bool HasValue => Color is not null || Glyph is not null || Bg is not null;
}
