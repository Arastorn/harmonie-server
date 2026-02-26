namespace Harmonie.Infrastructure.Dto;

public sealed class GuildMemberUserDto
{
    public Guid UserId { get; init; }

    public string Username { get; init; } = string.Empty;

    public string? DisplayName { get; init; }

    public string? AvatarUrl { get; init; }

    public bool IsActive { get; init; }

    public short Role { get; init; }

    public DateTime JoinedAtUtc { get; init; }
}
