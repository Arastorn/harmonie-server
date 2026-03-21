using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Interfaces.Common;

public interface IConnectionTracker
{
    Task HandleConnectedAsync(UserId userId, string connectionId, CancellationToken cancellationToken = default);
    Task HandleDisconnectedAsync(UserId userId, string connectionId, CancellationToken cancellationToken = default);
    bool IsOnline(UserId userId);
    IReadOnlyList<string> GetConnectionIds(UserId userId);
}
