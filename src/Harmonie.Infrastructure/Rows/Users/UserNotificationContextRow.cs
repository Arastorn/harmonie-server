namespace Harmonie.Infrastructure.Rows.Users;

sealed record UserNotificationContextRow(
    Guid UserId,
    Guid? GuildId,
    Guid? ConversationId);
