using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Common.Messages;

/// <summary>
/// Result returned by <see cref="PinnedMessageFetchOrchestrator"/>.
/// The caller maps this to the scope-specific response DTO.
/// </summary>
public sealed record PinnedMessageFetchResult(
    IReadOnlyList<GetPinnedMessagesItemResponse> Items,
    string? NextCursor);

/// <summary>
/// Shared orchestrator for GetPinnedMessages across all scopes.
/// Extracts cursor parsing, page fetching, and item mapping.
/// </summary>
public sealed class PinnedMessageFetchOrchestrator
{
    private const int DefaultLimit = 50;

    public async Task<ApplicationResponse<PinnedMessageFetchResult>> FetchAsync<TContext>(
        IPinnedMessageFetchScope<TContext> scope,
        string? rawCursor,
        int? rawLimit,
        UserId callerId,
        CancellationToken ct)
        where TContext : ScopeContext
    {
        // ── Cursor parsing ──────────────────────────────────────────────
        PinnedMessagesCursor? cursor = null;
        if (rawCursor is not null)
        {
            if (!PinnedMessagesCursorCodec.TryParse(rawCursor, out var parsed) || parsed is null)
            {
                return ApplicationResponse<PinnedMessageFetchResult>.Fail(
                    ApplicationErrorCodes.Common.ValidationFailed,
                    "Request validation failed",
                    EndpointExtensions.SingleValidationError(
                        "cursor",
                        ApplicationErrorCodes.Validation.InvalidFormat,
                        "Cursor is invalid"));
            }

            cursor = parsed;
        }

        var limit = Math.Clamp(rawLimit ?? DefaultLimit, 1, 100);

        // ── Authorization ───────────────────────────────────────────────
        var authResult = await scope.AuthorizeAsync(callerId, ct);
        if (authResult is AuthorizationResult<TContext>.Denied denied)
            return ApplicationResponse<PinnedMessageFetchResult>.Fail(denied.Error);

        // ── Fetch page ──────────────────────────────────────────────────
        var page = await scope.GetPinnedPageAsync(callerId, cursor, limit, ct);

        // ── Map items ───────────────────────────────────────────────────
        var items = page.Items
            .Select(x =>
            {
                page.AttachmentsByMessageId.TryGetValue(MessageId.From(x.MessageId), out var attachments);
                return new GetPinnedMessagesItemResponse(
                    MessageId: x.MessageId,
                    AuthorUserId: x.AuthorUserId,
                    AuthorUsername: x.AuthorUsername,
                    AuthorDisplayName: x.AuthorDisplayName,
                    Content: x.Content,
                    Attachments: attachments?.Select(MessageAttachmentDto.FromDomain).ToArray()
                                 ?? Array.Empty<MessageAttachmentDto>(),
                    CreatedAtUtc: x.CreatedAtUtc,
                    UpdatedAtUtc: x.UpdatedAtUtc,
                    PinnedByUserId: x.PinnedByUserId,
                    PinnedAtUtc: x.PinnedAtUtc);
            })
            .ToArray();

        // ── Build result ────────────────────────────────────────────────
        return ApplicationResponse<PinnedMessageFetchResult>.Ok(new PinnedMessageFetchResult(
            Items: items,
            NextCursor: page.NextCursor is null ? null : PinnedMessagesCursorCodec.Encode(page.NextCursor)));
    }
}
