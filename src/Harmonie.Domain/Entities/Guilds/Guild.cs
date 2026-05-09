using Harmonie.Domain.Common;
using Harmonie.Domain.ValueObjects.Common;
using Harmonie.Domain.ValueObjects.Guilds;
using Harmonie.Domain.ValueObjects.Uploads;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Domain.Entities.Guilds;

public sealed class Guild : Entity<GuildId>
{
    public GuildName Name { get; private set; }

    public UserId OwnerUserId { get; private set; }

    public UploadedFileId? IconFileId { get; private set; }

    /// <summary>
    /// Icon visual appearance (color, name/icon, background).
    /// </summary>
    public Appearance Icon { get; private set; } = Appearance.Empty;

    private Guild(
        GuildId id,
        GuildName name,
        UserId ownerUserId,
        UploadedFileId? iconFileId,
        Appearance icon,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        Name = name;
        OwnerUserId = ownerUserId;
        IconFileId = iconFileId;
        Icon = icon;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static Result<Guild> Create(
        GuildName name,
        UserId ownerUserId)
    {
        if (name is null)
            return Result.Failure<Guild>("Guild name is required");

        if (ownerUserId is null)
            return Result.Failure<Guild>("Owner user ID is required");

        var now = DateTime.UtcNow;
        var guild = new Guild(
            GuildId.New(),
            name,
            ownerUserId,
            iconFileId: null,
            icon: Appearance.Empty,
            now,
            now);

        return Result.Success(guild);
    }

    public static Guild Rehydrate(
        GuildId id,
        GuildName name,
        UserId ownerUserId,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        UploadedFileId? iconFileId = null,
        Appearance? icon = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(ownerUserId);

        return new Guild(
            id,
            name,
            ownerUserId,
            iconFileId,
            icon ?? Appearance.Empty,
            createdAtUtc,
            updatedAtUtc);
    }

    public Result UpdateName(GuildName newName)
    {
        if (Name == newName)
            return Result.Success();

        Name = newName;
        MarkAsUpdated();

        return Result.Success();
    }

    public Result UpdateIconFile(UploadedFileId? iconFileId)
    {
        IconFileId = iconFileId;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Update the guild's icon appearance (color, name/icon, background).
    /// </summary>
    public Result UpdateIcon(Appearance icon)
    {
        Icon = icon;
        MarkAsUpdated();
        return Result.Success();
    }
}
