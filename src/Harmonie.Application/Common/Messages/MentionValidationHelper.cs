using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Users;
using Harmonie.Domain.Common;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Common.Messages;

/// <summary>
/// Shared mention validation logic used by both <see cref="MessageSendOrchestrator"/>
/// and <see cref="MessageEditDeleteOrchestrator"/>.
/// </summary>
public static class MentionValidationHelper
{
    /// <summary>
    /// Validates mentioned user IDs: resolves GUIDs to UserIds, checks existence,
    /// verifies membership via the scope, and returns the validated distinct UserId array.
    /// Returns a failure ApplicationResponse with the appropriate error code on rejection.
    /// </summary>
    public static async Task<ApplicationResponse<UserId[]>> ValidateAsync<TContext>(
        IReadOnlyList<Guid> rawMentionedUserIds,
        IUserRepository userRepository,
        Func<IReadOnlyCollection<UserId>, TContext, CancellationToken, Task<Result>> validateMembershipAsync,
        TContext context,
        CancellationToken ct)
    {
        var distinctIds = rawMentionedUserIds.Distinct().ToArray();
        var userIds = distinctIds.Select(UserId.From).ToArray();

        var existingUsers = await userRepository.GetManyByIdsAsync(userIds, ct);
        var existingUserIds = existingUsers.Select(u => u.Id).ToHashSet();
        var missingIds = new List<Guid>();
        foreach (var id in userIds)
        {
            if (!existingUserIds.Contains(id))
                missingIds.Add(id.Value);
        }

        if (missingIds.Count > 0)
        {
            return ApplicationResponse<UserId[]>.Fail(
                ApplicationErrorCodes.Message.MentionedUserNotFound,
                $"One or more mentioned users were not found: {string.Join(", ", missingIds)}");
        }

        var validateResult = await validateMembershipAsync(userIds, context, ct);
        if (validateResult.IsFailure)
        {
            return ApplicationResponse<UserId[]>.Fail(
                ApplicationErrorCodes.Message.MentionedUserNotMember,
                validateResult.Error ?? "One or more mentioned users are not members of this scope");
        }

        return ApplicationResponse<UserId[]>.Ok(userIds);
    }
}
